using System;
using System.Linq;
using System.Collections.Generic;
using YamlDotNet.Serialization;

using datamodel.schema.tweaks;
using datamodel.utils;

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
            AssignLevel2_AndOfficialDocs(toc, source);
        }

        private static Toc ParseYaml(string url) {
            string yaml = SwaggerSource.DownloadUrl(url);

            IDeserializer deserializer = new DeserializerBuilder().Build();
            Toc toc = deserializer.Deserialize<Toc>(yaml);

            return toc;
        }

        private static void AssignLevel2_AndOfficialDocs(Toc toc, TempSource source) {
            const string prefix = "io.k8s.api.core.v1.";

            foreach (TocPart part in toc.parts) {
                foreach (TocChapter chapter in part.chapters) {
                    AddLinksToOfficialDocs(source, part, chapter);
                    string qualifiedName = prefix + chapter.name;
                    Model model = source.FindModel(qualifiedName);

                    if (model != null) {
                        foreach (Model include in model.SelfAndConnected())
                            // There is some opposing forces here. We've taken the position that primary grouping is as per the
                            // Fully Qualified Name hierarchy. However, the K8s Swagger TOC clearly violates this and puts entities
                            // into groups from different levels in this hierarchy.
                            if (include.QualifiedName.StartsWith(prefix))
                                include.SetLevel(1, part.name);
                    }
                }
            }
        }

        private static void AddLinksToOfficialDocs(TempSource source, TocPart part, TocChapter chapter) {
            string url = ToChapterUrl(part, chapter);

            // Main entity
            Model mainModel = FindModel(source, chapter, chapter.name);
            if (mainModel != null) {
                mainModel.AddUrl("Official Kubernetes Docs", url);
                // Console.WriteLine(url);      // Random sampled to confirm good links
            }

            // Other Definitions
            if (chapter.otherDefinitions != null)
                foreach (string otherDef in chapter.otherDefinitions) {
                    Model otherModel = FindModel(source, chapter, otherDef);
                    if (otherModel != null) {
                        string anchoredUrl = string.Format("{0}#{1}", url, otherDef);
                        otherModel.AddUrl("Official Kubernetes Docs", anchoredUrl);
                        // Console.WriteLine(anchoredUrl);      // Random sampled to confirm good links
                    }
                }
        }

        // Example qualified names:
        // 
        // io.k8s.api.core.v1.Pod
        // io.k8s.api.discovery.v1.EndpointSlice
        // io.k8s.api.autoscaling.v2.HorizontalPodAutoscaler
        //
        // Examples of chapter.group field:
        //
        // <blank>                          => io.k8s.api.core.v1.Pod
        // discovery.k8s.io                 => io.k8s.api.discovery.v1.EndpointSlice
        // autoscaling                      => io.k8s.api.autoscaling.v2.HorizontalPodAutoscaler
        // rbac.authorization.k8s.io        => io.k8s.api.rbac.v1.ClusterRole
        // flowcontrol.apiserver.k8s.io     => io.k8s.api.flowcontrol.v1beta2.FlowSchema
        // apiextensions.k8s.io             => io.k8s.apiextensions-apiserver.pkg.apis.apiextensions.v1.CustomResourceDefinition
        private static Model FindModel(TempSource source, TocChapter chapter, string name) {
            string group = chapter.group;
            string version = chapter.version;

            string fourthPart;

            if (group == null || group == "")
                fourthPart = "core";        // The default
            else
                fourthPart = group.Split('.').First();

            string qualifiedName;
            if (fourthPart == "apiextensions")
                qualifiedName = string.Format("io.k8s.apiextensions-apiserver.pkg.apis.{0}.{1}.{2}", fourthPart, version, name);
            else
                qualifiedName = string.Format("io.k8s.api.{0}.{1}.{2}", fourthPart, version, name);

            return source.FindModel(qualifiedName);
        }

        // The following snippet of YAML from the "Part" level...
        // - name: Authorization Resources
        //   chapters:
        //   - name: LocalSubjectAccessReview
        //     group: authorization.k8s.io
        //     version: v1
        //
        // Maps to the following URL...
        // https://kubernetes.io/docs/reference/kubernetes-api/authorization-resources/local-subject-access-review-v1/
        private static string ToChapterUrl(TocPart part, TocChapter chapter) {
            return string.Format("https://kubernetes.io/docs/reference/kubernetes-api/{0}/{1}-{2}/",
                part.name.ToLower().Replace(" ", "-"),
                NameUtils.ToHuman(chapter.name).ToLower().Replace(" ", "-"),
                chapter.version);
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