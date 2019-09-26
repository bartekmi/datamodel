using System;

namespace datamodel.metadata {
    public static class TeamInfo {
        // Once this is integrated, add a color for each team in teams.json
        public static string GetHtmlColorForTeam(string team) {
            if (team == null)
                return "#d0d0d0";

            Random random = new Random(team.GetHashCode());

            return string.Format("#{0}{1}{2}",
                RandomColorComponent(random),
                RandomColorComponent(random),
                RandomColorComponent(random)
            );
        }

        private static string RandomColorComponent(Random random) {
            double range = 0.2;

            double relativeValue = 1.0 - random.NextDouble() * range;
            int absoluteValue = (int)(relativeValue * 256);

            return absoluteValue.ToString("X2");
        }
    }
}