/* SAM Auto 9.0 — Free Games: Steam network client (SteamKit2)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.Authentication;
using SteamKit2.Internal;

namespace SAM.Picker.FreeGames
{
    /// <summary>
    /// Wraps a SteamKit2 CM connection for the Free Games feature:
    /// QR-code sign-in (no password needed), token re-login, and
    /// ClientRequestFreeLicense batching. The running Steam client is
    /// not involved — this is an independent connection.
    /// </summary>
    internal sealed class SteamFreeClient : IDisposable
    {
        public enum ClientState
        {
            Disconnected,
            Connecting,
            AwaitingQrScan,
            LoggingOn,
            LoggedIn,
        }

        public event Action<ClientState> StateChanged;
        public event Action<string> Log;

        private SteamClient _client;
        private CallbackManager _manager;
        private Thread _callbackThread;
        private volatile bool _pumping;

        private SteamUser _user;
        private SteamApps _apps;

        private TaskCompletionSource<bool> _connected;
        private TaskCompletionSource<SteamUser.LoggedOnCallback> _loggedOn;
        private QrAuthSession _qrSession;

        public ClientState State { get; private set; } = ClientState.Disconnected;
        public bool IsLoggedIn { get; private set; }
        public string AccountName { get; private set; }
        public string RefreshToken { get; private set; }
        public string AccessToken { get; private set; }
        public ulong SteamId { get; private set; }

        #region Connection lifecycle

        private void SetState(ClientState state)
        {
            this.State = state;
            this.StateChanged?.Invoke(state);
        }

        private void EnsureClient()
        {
            if (this._client != null)
            {
                return;
            }

            this._client = new SteamClient();
            this._manager = new CallbackManager(this._client);

            this._manager.Subscribe<SteamClient.ConnectedCallback>(this.OnConnected);
            this._manager.Subscribe<SteamClient.DisconnectedCallback>(this.OnDisconnected);
            this._manager.Subscribe<SteamUser.LoggedOnCallback>(this.OnLoggedOn);
            this._manager.Subscribe<SteamUser.LoggedOffCallback>(this.OnLoggedOff);

            this._user = this._client.GetHandler<SteamUser>();
            this._apps = this._client.GetHandler<SteamApps>();

            this._pumping = true;
            this._callbackThread = new(this.CallbackLoop)
            {
                IsBackground = true,
                Name = "SAM-Auto FreeGames callbacks",
            };
            this._callbackThread.Start();
        }

        private void CallbackLoop()
        {
            while (this._pumping == true)
            {
                try
                {
                    this._manager.RunWaitCallbacks(TimeSpan.FromMilliseconds(250));
                }
                catch (Exception e)
                {
                    this.Log?.Invoke("callback pump: " + e.Message);
                }
            }
        }

        private void OnConnected(SteamClient.ConnectedCallback callback)
        {
            this._connected?.TrySetResult(true);
        }

        private void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            this.IsLoggedIn = false;
            this._connected?.TrySetCanceled();
            this._loggedOn?.TrySetException(new Exception("disconnected"));
            this.SetState(ClientState.Disconnected);
        }

        private void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            this._loggedOn?.TrySetResult(callback);
        }

        private void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            this.IsLoggedIn = false;
            this.SetState(ClientState.Disconnected);
        }

        private async Task EnsureConnectedAsync(CancellationToken cancellation)
        {
            this.EnsureClient();

            if (this._client.IsConnected == true)
            {
                return;
            }

            this.SetState(ClientState.Connecting);
            this._connected = new(TaskCreationOptions.RunContinuationsAsynchronously);
            this._client.Connect();

            var completed = await Task.WhenAny(
                this._connected.Task,
                Task.Delay(TimeSpan.FromSeconds(20), cancellation)).ConfigureAwait(false);

            if (completed != this._connected.Task)
            {
                throw new Exception("connection to Steam timed out");
            }
        }

        #endregion

        #region QR sign-in

        /// <summary>Begins a QR auth session and returns the challenge URL to render.</summary>
        public async Task<string> BeginQrLoginAsync(CancellationToken cancellation)
        {
            await this.EnsureConnectedAsync(cancellation).ConfigureAwait(false);

            this._qrSession = await this._client.Authentication.BeginAuthSessionViaQRAsync(new()
            {
                DeviceFriendlyName = "SAM Auto 9.0",
                PlatformType = EAuthTokenPlatformType.k_EAuthTokenPlatformType_SteamClient,
            }).ConfigureAwait(false);

            this.SetState(ClientState.AwaitingQrScan);
            return this._qrSession.ChallengeURL;
        }

        /// <summary>
        /// Polls until the user confirms the QR login in the Steam mobile app,
        /// then logs on. Returns the account name.
        /// </summary>
        public async Task<string> CompleteQrLoginAsync(CancellationToken cancellation)
        {
            var session = this._qrSession ?? throw new InvalidOperationException("no QR session in progress");

            AuthPollResult result = null;
            while (result == null)
            {
                cancellation.ThrowIfCancellationRequested();
                await Task.Delay(session.PollingInterval, cancellation).ConfigureAwait(false);
                result = await session.PollAuthSessionStatusAsync().ConfigureAwait(false);
            }

            this.AccountName = result.AccountName;
            this.RefreshToken = result.RefreshToken;
            this.AccessToken = result.AccessToken;

            await this.LogOnAsync(result.AccountName, result.RefreshToken, cancellation).ConfigureAwait(false);
            return this.AccountName;
        }

        /// <summary>Logs on with a previously saved refresh token.</summary>
        public async Task LoginWithTokenAsync(string accountName, string refreshToken, CancellationToken cancellation)
        {
            await this.LogOnAsync(accountName, refreshToken, cancellation).ConfigureAwait(false);
            this.AccountName = accountName;
            this.RefreshToken = refreshToken;

            // generate a fresh access token for store web requests
            try
            {
                var tokens = await this._client.Authentication
                    .GenerateAccessTokenForAppAsync(new(this.SteamId), refreshToken)
                    .ConfigureAwait(false);
                this.AccessToken = tokens.AccessToken;
            }
            catch (Exception e)
            {
                this.Log?.Invoke("access token generation failed: " + e.Message);
            }
        }

        private async Task LogOnAsync(string accountName, string accessToken, CancellationToken cancellation)
        {
            await this.EnsureConnectedAsync(cancellation).ConfigureAwait(false);

            this.SetState(ClientState.LoggingOn);
            this._loggedOn = new(TaskCreationOptions.RunContinuationsAsynchronously);

            this._user.LogOn(new()
            {
                Username = accountName,
                AccessToken = accessToken,
                ShouldRememberPassword = true,
            });

            var completed = await Task.WhenAny(
                this._loggedOn.Task,
                Task.Delay(TimeSpan.FromSeconds(30), cancellation)).ConfigureAwait(false);

            if (completed != this._loggedOn.Task)
            {
                throw new Exception("logon timed out");
            }

            var result = await this._loggedOn.Task.ConfigureAwait(false);
            if (result.Result != EResult.OK)
            {
                throw new Exception("logon failed: " + result.Result + " / " + result.ExtendedResult);
            }

            this.SteamId = result.ClientSteamID != null ? result.ClientSteamID.ConvertToUInt64() : this.SteamId;
            this.IsLoggedIn = true;
            this.SetState(ClientState.LoggedIn);
        }

        #endregion

        #region Free licenses

        /// <summary>
        /// Requests free licenses for a batch of appids.
        /// Returns the callback result and the list of newly granted appids.
        /// </summary>
        public async Task<(EResult Result, int GrantedCount)> RequestFreeLicensesAsync(uint[] appIds)
        {
            if (this.IsLoggedIn == false || this._apps == null)
            {
                throw new InvalidOperationException("not logged in");
            }

            var job = this._apps.RequestFreeLicense(appIds).ToTask();
            var completed = await Task.WhenAny(job, Task.Delay(TimeSpan.FromSeconds(45))).ConfigureAwait(false);
            if (completed != job)
            {
                return (EResult.Timeout, 0);
            }

            var callback = await job.ConfigureAwait(false);
            return (callback.Result, callback.GrantedApps?.Count ?? 0);
        }

        #endregion

        public void Disconnect()
        {
            try
            {
                if (this.IsLoggedIn == true)
                {
                    this._user?.LogOff();
                }
            }
            catch
            {
                // ignore
            }

            try
            {
                this._client?.Disconnect();
            }
            catch
            {
                // ignore
            }

            this.IsLoggedIn = false;
            this.SetState(ClientState.Disconnected);
        }

        public void Dispose()
        {
            this._pumping = false;

            try
            {
                this.Disconnect();
            }
            catch
            {
                // ignore
            }

            try
            {
                this._callbackThread?.Join(TimeSpan.FromSeconds(2));
            }
            catch
            {
                // ignore
            }
        }
    }
}
