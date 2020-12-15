using System.Collections.Generic;

namespace Rinsen.Outback
{
    /// <summary>
    /// Helper for validating string with Augmented Backus–Naur form
    /// </summary>
    public class AbnfValidationHelper
    {
        private static readonly HashSet<char> _validCaracters = new HashSet<char>()
        {
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
            '-', '.', '_', '~', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
        };

        /// <summary>
        /// Validation rules
        /// unreserved = ALPHA / DIGIT / "-" / "." / "_" / "~"
        /// ALPHA = %x41-5A / %x61-7A
        /// DIGIT = % x30 - 39
        /// </summary>
        /// <param name="value">String to validate</param>
        /// <returns></returns>
        public static bool IsValid(string value)
        {
            foreach (var character in value)
            {
                if (!_validCaracters.Contains(character))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validation rules
        /// unreserved = ALPHA / DIGIT / "-" / "." / "_" / "~"
        /// ALPHA = %x41-5A / %x61-7A
        /// DIGIT = % x30 - 39
        /// </summary>
        /// <param name="value">String to validate</param>
        /// <param name="minLength">Valid string min length</param>
        /// <param name="maxLength">Valid string max length</param>
        /// <returns></returns>
        public static bool IsValid(string value, int minLength, int maxLength)
        {
            if (value.Length > maxLength || value.Length < minLength)
            {
                return false;
            }

            return IsValid(value);
        }

    }
}
