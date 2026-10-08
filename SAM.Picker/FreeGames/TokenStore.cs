/* SAM Auto 9.0 — Free Games: encrypted session token storage
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 */

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SAM.Picker.FreeGames
{
    /// <summary>
    /// Persists the Steam refresh token encrypted with DPAPI (current user scope),
    /// so the QR sign-in is only needed once on this machine.
    /// </summary>
    internal static class TokenStore
    {
        private static string TokenFile => Path.Combine(Lang.DataDirectory, "freegames.session");

        public static void Save(string accountName, string refreshToken)
        {
            try
            {
                var plain = Encoding.UTF8.GetBytes(accountName + "\n" + refreshToken);
                var encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(TokenFile, encrypted);
            }
            catch
            {
                // best effort — failing to save only means QR is needed again next time
            }
        }

        public static bool TryLoad(out string accountName, out string refreshToken)
        {
            accountName = null;
            refreshToken = null;

            try
            {
                if (File.Exists(TokenFile) == false)
                {
                    return false;
                }

                var encrypted = File.ReadAllBytes(TokenFile);
                var plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                var text = Encoding.UTF8.GetString(plain);
                var split = text.IndexOf('\n');
                if (split <= 0)
                {
                    return false;
                }

                accountName = text.Substring(0, split);
                refreshToken = text.Substring(split + 1);
                return string.IsNullOrWhiteSpace(accountName) == false &&
                       string.IsNullOrWhiteSpace(refreshToken) == false;
            }
            catch
            {
                return false;
            }
        }

        public static void Clear()
        {
            try
            {
                if (File.Exists(TokenFile) == true)
                {
                    File.Delete(TokenFile);
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}
