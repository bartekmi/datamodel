using System;
using System.Linq;

namespace datamodel.utils {
    public static class NameUtils {
        public static string Camelize(string snakeCase) {

            string[] pieces = snakeCase.Split('_');
            return string.Join("", pieces.Select(x => Capitalize(x)));
        }

        public static string Capitalize(string text) {
            if (string.IsNullOrEmpty(text))
                return text;

            return
                char.ToUpper(text[0]).ToString() +
                text.Substring(1);
        }


        private static readonly string[] NON_CHANGE_EXCEPTIONS = new string[] { "ag_grid_settings" };

        public static string Pluralize(string singular) {
            if (NON_CHANGE_EXCEPTIONS.Contains(singular))
                return singular;
            if (singular.EndsWith("y"))
                return singular.Substring(0, singular.Length - 1) + "ies";
            if (singular.EndsWith("ss"))
                return singular + "es";
            return singular + "s";
        }

        public static string Singularize(string plural) {
            if (NON_CHANGE_EXCEPTIONS.Contains(plural))
                return plural;
            if (plural.EndsWith("ies"))
                return plural.Substring(0, plural.Length - 3) + "y";
            if (plural.EndsWith("sses"))
                return plural.Substring(0, plural.Length - 2);
            return plural.Substring(0, plural.Length - 1);
        }

        public static string SnakeCaseToHuman(string snake_case) {
            string[] pieces = snake_case.Split('_');
            return string.Join(" ", pieces.Select(x => Capitalize(x)));
        }
    }
}