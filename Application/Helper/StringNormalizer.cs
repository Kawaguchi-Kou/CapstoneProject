using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Application.Helper
{
    public static class StringNormalizer
    {
        public static string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            input = input.ToLower().Trim();

            // Remove accents
            var normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            input = sb.ToString().Normalize(NormalizationForm.FormC);

            // Replace đ
            input = input.Replace('đ', 'd');

            // Remove numbers
            input = Regex.Replace(input, @"\d+", "");

            // Remove special characters
            input = Regex.Replace(input, @"[^a-z\s]", "");

            // Remove spaces
            input = input.Replace(" ", "");

            return input;
        }
    }
}
