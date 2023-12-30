using System.Security.Cryptography;
using System.Text;

namespace Harckade.CMS.Utils
{
    public static class Hash
    {
        public static string Sha512(string input)
        {
            var hash = string.Empty;
            var data = Encoding.ASCII.GetBytes(input);
            using (var sha512 = SHA512.Create())
            {
                var hashedBytes = sha512.ComputeHash(data);
                hash = BitConverter.ToString(hashedBytes).Replace("-", "");
            }
            if (string.IsNullOrEmpty(hash))
            {
                throw new CryptographicException(nameof(hash));
            }
            return hash;
        }

        public static string Sha256(string input)
        {
            var hash = string.Empty;
            var data = Encoding.ASCII.GetBytes(input);
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(data);
                hash = BitConverter.ToString(hashedBytes).Replace("-", "");
            }
            if (string.IsNullOrEmpty(hash))
            {
                throw new CryptographicException(nameof(hash));
            }
            return hash;
        }
    }
}
