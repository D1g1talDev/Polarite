using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Polarite.Networking.Extensions
{
    public static class StringExtensions
    {
        private static readonly Regex tmpTagRegex = new Regex("<.*?>", RegexOptions.Compiled);
        public static string WithoutTMP(this string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            return tmpTagRegex.Replace(str, string.Empty);
        }
    }
}
