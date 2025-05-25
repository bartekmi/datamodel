using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

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

        public static string ToHuman(string nonHuman, bool dropSpaces = false) {
            if (nonHuman == null)
                return null;

            string human = null;
            if (nonHuman.Contains('_'))
                human = SnakeCaseToHuman(nonHuman);
            else if (nonHuman.Contains('-'))
                human = SnakeCaseToHuman(nonHuman, '-');
            else
                human = MixedCaseToHuman(nonHuman);

            return dropSpaces ? human.Replace(" ", "") : human;
        }

        public static IEnumerable<string> ToWords(string nonHuman) {
            if (nonHuman == null)
                return null;

            return ToHuman(nonHuman).Split(" ")
                .Select(x => x.ToLower());
        }

        private static string SnakeCaseToHuman(string snake_case, char splitChar = '_') {
            string[] pieces = snake_case.Split(splitChar);
            return string.Join(" ", pieces.Select(x => Capitalize(x)));
        }

        private static string MixedCaseToHuman(string text) {
            StringBuilder builder = new StringBuilder();

            for (int ii = 0; ii < text.Length; ii++) {
                bool prevLower = ii > 0 && char.IsLower(text[ii - 1]);
                bool prevUpper = ii > 0 && char.IsUpper(text[ii - 1]);
                bool nextLower = ii < text.Length - 1 && char.IsLower(text[ii + 1]);
                bool upper = char.IsUpper(text[ii]);

                if (prevLower && upper || prevUpper && upper && nextLower)
                    builder.Append(' ');
                builder.Append(text[ii]);
            }

            return Capitalize(builder.ToString());
        }

        public static string CompoundToSafe(IEnumerable<string> compound) {
            return string.Join("__", compound).Replace(' ', '_');
        }

        public static string Pluralize(string singular) {
            if (singular.EndsWith("s"))     // Bonus => Bonuses
                return singular + "es";
            if (singular.EndsWith("y"))     // Country => Countries
                return singular[..^1] + "ies";
            return singular + "s";          // Cat => Cats
        }

        public static string Singluarize(string plural) {
            if (plural.EndsWith("ses"))     // Bonuses => Bonus
                return plural[..^2];
            if (plural.EndsWith("ies"))     // Countries => Country
                return plural[..^3] + "y";
            if (plural.EndsWith("s"))
                return plural[..^1];        // Cats => Cat
            return plural;                  // Sheep => Sheep :)
        }
    }
}