using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using datamodel.schema.source;

namespace datamodel.schema.tweaks {
    internal static class TweakLoader {
        private static Type[] TweakTypes = new Type[] {
            typeof(AddBaseClassTweak),
            typeof(AddInheritanceTweak),
            typeof(MoveDerivedToPeerLevelTweak),
        };

        internal static void Load(SchemaSource source, string[] jsons) {
            foreach (string json in jsons)
                source.Tweaks = source.Tweaks.Concat(Load(json)).ToList();
        }

        private static List<Tweak> Load(string json) {
            List<TweakInfo> infos = JsonConvert.DeserializeObject<List<TweakInfo>>(json);

            List<Tweak> tweaks = new List<Tweak>();
            foreach (TweakInfo info in infos)
                tweaks.Add(LoadTweak(info));

            return tweaks;
        }

        private static Tweak LoadTweak(TweakInfo info) {
            Type type = TweakTypes.SingleOrDefault(x => x.Name == info.Type);
            if (type == null)
                throw new Exception(string.Format("Unknown tweak type '{0}'. Known types: {1}",
                    info.Type,
                    string.Join(", ", TweakTypes.Select(x => x.Name))));

            JToken json = info.Spec as JToken;
            if (json == null)
                throw new Exception("No spec for " + info.Type);

            return (Tweak)json.ToObject(type);
        }

    }

    public class TweakInfo {
        public string Type;
        public object Spec;
    }
}