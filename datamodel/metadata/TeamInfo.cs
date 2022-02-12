using System;

namespace datamodel.metadata {
    public static class TeamInfo {
        public static string GetHtmlColorForTeam(string team) {
            // TODO: Need an injection mechanism for mapping Level1 to a color
            return "lightgrey";
        }
    }
}