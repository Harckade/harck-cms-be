using System.Globalization;
using System.Text.RegularExpressions;

namespace Harckade.CMS.Utils
{
    public static class Validations
    {
        /// <summary>
        /// Taken from here: https://docs.microsoft.com/en-us/dotnet/standard/base-types/how-to-verify-that-strings-are-in-valid-email-format
        /// Please consult original license
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Normalize the domain
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                                      RegexOptions.None, TimeSpan.FromMilliseconds(200));

                // Examines the domain part of the email and normalizes it.
                string DomainMapper(Match match)
                {
                    // Use IdnMapping class to convert Unicode domain names.
                    var idn = new IdnMapping();

                    // Pull out and process domain name (throws ArgumentException on invalid)
                    string domainName = idn.GetAscii(match.Groups[2].Value);

                    return match.Groups[1].Value + domainName;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static bool IsValidSHA512(string input)
        {
            string pattern = "^[A-Fa-f0-9]{128}$";
            return Regex.IsMatch(input, pattern);
        }

        #region Microsoft.Azure.Storage

        private static readonly RegexOptions RegexOptions = RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.CultureInvariant;

        private static readonly string[] ReservedFileNames = new string[25]
{
            ".", "..", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8",
            "LPT9", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "PRN", "AUX", "NUL", "CON", "CLOCK$"
};

        private static readonly Regex FileDirectoryRegex = new Regex("^[^\"\\\\/:|<>*?]*\\/{0,1}$", RegexOptions);

        //
        // Summary:
        //     Checks if a file name is valid.
        //
        // Parameters:
        //   fileName:
        //     A string representing the file name to validate.
        // Taken from Microsoft.Azure.Storage. The original package was deprecated and no replacement was provided for this particular method.
        public static void ValidateFileName(string fileName)
        {
            ValidateFileDirectoryHelper(fileName, "file");
            if (fileName.EndsWith("/", StringComparison.Ordinal))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid {0} name. Check MSDN for more information about valid {0} naming.", "file"));
            }

            string[] reservedFileNames = ReservedFileNames;
            for (int i = 0; i < reservedFileNames.Length; i++)
            {
                if (reservedFileNames[i].Equals(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid {0} name. This {0} name is reserved.", "file"));
                }
            }
        }

        //
        // Summary:
        //     Checks if a blob name is valid.
        //
        // Parameters:
        //   blobName:
        //     A string representing the blob name to validate.
        // Taken from Microsoft.Azure.Storage. The original package was deprecated and no replacement was provided for this particular method.
        public static void ValidateBlobName(string blobName)
        {
            if (string.IsNullOrWhiteSpace(blobName))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid {0} name. The {0} name may not be null, empty, or whitespace only.", "blob"));
            }

            if (blobName.Length < 1 || blobName.Length > 1024)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid {0} name length. The {0} name must be between {1} and {2} characters long.", "blob", 1, 1024));
            }

            int num = 0;
            for (int i = 0; i < blobName.Length; i++)
            {
                if (blobName[i] == '/')
                {
                    num++;
                }
            }

            if (num >= 254)
            {
                throw new ArgumentException("The count of URL path segments (strings between '/' characters) as part of the blob name cannot exceed 254.");
            }
        }

        private static void ValidateFileDirectoryHelper(string resourceName, string resourceType)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid {0} name. The {0} name may not be null, empty, or whitespace only.", resourceType));
            }

            if (resourceName.Length < 1 || resourceName.Length > 255)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid {0} name length. The {0} name must be between {1} and {2} characters long.", resourceType, 1, 255));
            }

            if (!FileDirectoryRegex.IsMatch(resourceName))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid {0} name. Check MSDN for more information about valid {0} naming.", resourceType));
            }
        }
        #endregion
    }
}
