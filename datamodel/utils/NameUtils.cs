using System;
using System.Text;
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

        public static string SnakeCaseToHuman(string snake_case) {
            string[] pieces = snake_case.Split('_');
            return string.Join(" ", pieces.Select(x => Capitalize(x)));
        }

        public static string MixedCaseToHuman(string text) {
            bool previousWasLower = false;
            StringBuilder builder = new StringBuilder();

            foreach (char c in text) {
                if (previousWasLower) {
                    if (char.IsUpper(c)) {
                        builder.Append(" ");
                        previousWasLower = false;
                    }
                } else {
                    if (char.IsLower(c))
                        previousWasLower = true;
                }
                builder.Append(c);
            }

            return builder.ToString();
        }
    }
}