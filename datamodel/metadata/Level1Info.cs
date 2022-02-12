using System;
using System.Collections.Generic;

namespace datamodel.metadata {
    // Here are some appropriate colors you can choose from...
    // skyblue, #c0c0c0, orchid, lightsalmon, #00ff00, lemonchiffon, #b8860b, #98fb98. greenyellow, 
    // #ffa500, deepskyblue, cyan, palevioletred, darkgoldenrod, #8fbc8f, honeydew, #ffff00, 
    public static class Level1Info {
        private static Dictionary<string, string> _level1_ToColor = new Dictionary<string, string>();

        public static void AssignColor(string level1, string color) {
            _level1_ToColor[level1] = color;
        }

        public static string GetHtmlColorForLevel1(string level1) {
            if (_level1_ToColor.TryGetValue(level1, out string color))
                return color;

            return "lightgrey";     // The default color
        }
    }
}