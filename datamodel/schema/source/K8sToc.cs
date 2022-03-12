using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

using datamodel.schema.tweaks;

namespace datamodel.schema.source {
    public class K8sTocTweak : Tweak {
        public override void Apply(TempSource source) {
            K8sToc.AssignCoreLevel2Groups(source);
        }
    }

    // The Json Swagger K8s file does not contain enough information to group the models.
    // This information is in a companion YAML file - see URL below.
    // The description of the official doc-building process is here:
    // https://github.com/kubernetes/website#building-the-api-reference-pages
    public static class K8sToc {
        const string TOC_URL = "https://raw.githubusercontent.com/kubernetes/website/main/api-ref-assets/config/toc.yaml";

        public static void AssignCoreLevel2Groups(TempSource source) {
            Toc toc = ParseYaml(TOC_URL);
            AssignLevel2Groups(toc, source);
        }

        private static Toc ParseYaml(string url) {
            string yaml = SwaggerSource.DownloadUrl(url);

            IDeserializer deserializer = new DeserializerBuilder().Build();
            Toc toc = deserializer.Deserialize<Toc>(yaml);

            return toc;
        }

        private static void AssignLevel2Groups(Toc toc, TempSource source) {
            const string prefix = "io.k8s.api.core.v1.";

            foreach (TocPart part in toc.parts) {
                HashSet<Model> models = new HashSet<Model>();

                foreach (TocChapter chapter in part.chapters) {
                    string qualifiedName = prefix + chapter.name;
                    Model model = source.FindModel(qualifiedName);

                    if (model != null)
                        foreach (Model toAdd in model.SelfAndConnected())
                            models.Add(toAdd);
                }

                // Now that we've visited all the chapters, anything in the HashSet belongs
                // to this "part". But filter out things that are not in core.
                foreach (Model model in models) {
                    if (model.QualifiedName.StartsWith(prefix))
                        model.Level2 = part.name;
                }
            }
        }
    }

    public class Toc {
        public List<TocPart> parts;
        public List<string> skippedResources;
    }

    public class TocPart {
        public string name;
        public List<TocChapter> chapters;
    }

    public class TocChapter {
        public string name;
        public string key;
        public string group;
        public string version;
        public List<string> otherDefinitions;
    }
}