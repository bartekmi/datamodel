using System;

namespace datamodel.metadata {
    public static class TeamInfo {
        // Once this is integrated, add a color for each team in teams.json
        public static string GetHtmlColorForTeam(string team) {
            switch (team) {
                case "air": return "skyblue";
                case "backend_infra": return "#c0c0c0";
                case "bid_app": return "orchid";
                case "bookings": return "lightsalmon";
                case "customs": return "#00ff00";
                case "dot_org": return "lemonchiffon";
                case "enterprise_enablement": return "#ffe4e1";
                case "finance": return "#98fb98";
                case "growth": return "greenyellow";
                case "marketplace": return "#ffa500";
                case "ocean_fcl": return "deepskyblue";
                case "ocean_lcl": return "cyan";
                case "quoting": return "palevioletred";
                case "shipment_activity": return "#f0e68c";
                case "shipment_data": return "#8fbc8f";
                case "squad_tools": return "honeydew";
                case "trucking": return "#ffff00";
            }

            return "lightgrey";
        }
    }
}