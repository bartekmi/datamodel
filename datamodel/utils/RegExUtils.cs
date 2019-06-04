using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace datamodel.utils {
    public static class RegExUtils {

        // Good resource on RegEx in C#:
        // https://docs.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.match.groups?view=netframework-4.8
        public static string[] GetCaptureGroups(string line, string pattern, Func<string, Exception> exceptionMaker) {
            Match match = Regex.Match(line, pattern);
            if (!match.Success)
                if (exceptionMaker == null)
                    return null;
                else
                    throw exceptionMaker(string.Format("No match for {0} in {1} at line {2}", pattern, line));

            return match.Groups.Skip(1).Select(x => x.ToString()).ToArray();
        }
    }
}