using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace datamodel.utils {
    public static class RegExUtils {

        // Good resource on RegEx in C#:
        // https://docs.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.match.groups?view=netframework-4.8
        // Only supports single instance of each capture group
        public static string[] GetCaptureGroups(string text, string pattern) {
            Match match = Regex.Match(text, pattern);
            if (!match.Success)
                return null;

            var groups = (IEnumerable<Group>)match.Groups;
            return groups.Skip(1).Select(x => x.ToString()).ToArray();
        }

        // For a regex that has a single capture group but expects multiple results in it
        public static string[] GetMultipleCaptures(string text, string pattern) {
            Match match = Regex.Match(text, pattern);
            if (!match.Success || match.Groups.Count != 2)
                return null;

            Group group = match.Groups[1];
            return group.Captures.Select(x => x.ToString()).ToArray();
        }

        public static string Replace(string input, string pattern, string replacement) {
            return Regex.Replace(input, pattern, replacement);
        }
    }
}