using System;
using System.Collections.Generic;

namespace datamodel.metadata {
    // Here are some appropriate colors you can choose from...
    // skyblue, #c0c0c0, orchid, lightsalmon, #00ff00, lemonchiffon, #b8860b, #98fb98. greenyellow, 
    // #ffa500, deepskyblue, cyan, palevioletred, darkgoldenrod, #8fbc8f, honeydew, #ffff00, 

    public static class Level1Info {

        private static string[] COLORS = new string[] {
            "skyblue", "#c0c0c0", "orchid", "lightsalmon", "#00ff00", "lemonchiffon", "#b8860b", "#98fb98", "greenyellow",
            "#ffa500", "deepskyblue", "cyan", "palevioletred", "darkgoldenrod", "#8fbc8f", "honeydew", "#ffff00",
        };

        private static Dictionary<string, string> _level1_ToColor = new Dictionary<string, string>();

        public static void AssignColor(string level1, string color) {
            _level1_ToColor[level1] = color;
        }

        public static string GetHtmlColorForLevel1(string level1) {
            if (string.IsNullOrEmpty(level1))
                return "lightgrey";     // The default color

            if (level1 != null && _level1_ToColor.TryGetValue(level1, out string color))
                return color;

            // This is somewhat lame because colors will collide, but also very low hanging fruit
            // Next step would be to initialize this class with all lavel1 strings and avoid collisions
            return COLORS[Math.Abs(level1.GetDeterministicHashCode()) % COLORS.Length];
        }

        // https://andrewlock.net/why-is-string-gethashcode-different-each-time-i-run-my-program-in-net-core/
        static int GetDeterministicHashCode(this string str) {
            unchecked {
                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length; i += 2) {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1)
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }
    }
}