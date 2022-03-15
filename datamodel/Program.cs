using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

using datamodel.metadata;
using datamodel.schema;
using datamodel.schema.source;
using datamodel.schema.tweaks;
using datamodel.datadict;
using datamodel.toplevel;
using datamodel.utils;
using datamodel.graphviz;

[assembly: InternalsVisibleTo("datamodel_test2")]

namespace datamodel {

    class Program {

        //********************************************************************************
        // Once files are generated, use this command to start local web server:
        //
        //  python -m http.server 80             # Python 3
        //
        //********************************************************************************

        static void Main(string[] args) {
            Env.Configure();
            Error.Clear();

            var options = new SwaggerSourceOptions() {
                BoringNameComponents = new string[] {
                    "io", "k8s", "api", "pkg", "v1", "v1alpha1", "v1beta1", "v1beta2", "v2", "v2beta1", "v2beta2"
                },
            };

            //SimpleSource source = new SimpleSource("../datamodel_test2/schema/simple_schema.json");   // Path is relative to 'CWD' attribute in launch.json
            //SwaggerSource source = K8sSwaggerSource.FromFile("../datamodel_test2/schema/swagger_schema.json", options);

            string json = SwaggerSource.DownloadUrl("https://raw.githubusercontent.com/kubernetes/kubernetes/master/api/openapi-spec/swagger.json");
            SwaggerSource source = new K8sSwaggerSource(json, options);

            source.PreHydrationTweaks = new List<Tweak>() {
                new FilterOldApiVersionsTweak(),
                new AddBaseClassTweak() {
                    BaseClassName = "AbstractContainer",
                    DerviedQualifiedNames = new string[] {
                        "io.k8s.api.core.v1.Container",
                        "io.k8s.api.core.v1.EphemeralContainer",
                    }
                },
                new AddBaseClassTweak() {
                    BaseClassName = "Webhook",
                    DerviedQualifiedNames = new string[] {
                        "io.k8s.api.admissionregistration.v1.MutatingWebhook",
                        "io.k8s.api.admissionregistration.v1.ValidatingWebhook",
                    }
                },
                new AddBaseClassTweak() {
                    BaseClassName = "PersistentVolumeSource",
                    BaseClassDescription = "Base for (only) Persistent Volume Sources",
                    PromoteIncomingAssociations = true,
                    DerviedQualifiedNames = new string[] {
                        "io.k8s.api.core.v1.AzureFilePersistentVolumeSource",
                        "io.k8s.api.core.v1.CephFSPersistentVolumeSource",
                        "io.k8s.api.core.v1.CinderPersistentVolumeSource",
                        "io.k8s.api.core.v1.CSIPersistentVolumeSource",
                        "io.k8s.api.core.v1.FlexPersistentVolumeSource",
                        "io.k8s.api.core.v1.GlusterfsPersistentVolumeSource",
                        "io.k8s.api.core.v1.ISCSIPersistentVolumeSource",
                        "io.k8s.api.core.v1.LocalVolumeSource",
                        "io.k8s.api.core.v1.RBDPersistentVolumeSource",
                        "io.k8s.api.core.v1.ScaleIOPersistentVolumeSource",
                        "io.k8s.api.core.v1.StorageOSPersistentVolumeSource",
                    }
                },
                new AddBaseClassTweak() {
                    BaseClassName = "VolumeSource",
                    BaseClassDescription = "Base for (only) Volume Sources",
                    PromoteIncomingAssociations = true,
                    DerviedQualifiedNames = new string[] {
                        "io.k8s.api.core.v1.AzureFileVolumeSource",
                        "io.k8s.api.core.v1.CephFSVolumeSource",
                        "io.k8s.api.core.v1.CinderVolumeSource",
                        "io.k8s.api.core.v1.ConfigMapVolumeSource",
                        "io.k8s.api.core.v1.CSIVolumeSource",
                        "io.k8s.api.core.v1.DownwardAPIVolumeSource",
                        "io.k8s.api.core.v1.EmptyDirVolumeSource",
                        "io.k8s.api.core.v1.EphemeralVolumeSource",
                        "io.k8s.api.core.v1.FlexVolumeSource",
                        "io.k8s.api.core.v1.GCEPersistentDiskVolumeSource",
                        "io.k8s.api.core.v1.GitRepoVolumeSource",
                        "io.k8s.api.core.v1.GlusterfsVolumeSource",
                        "io.k8s.api.core.v1.ISCSIVolumeSource",
                        "io.k8s.api.core.v1.PersistentVolumeClaimVolumeSource",
                        "io.k8s.api.core.v1.ProjectedVolumeSource",
                        "io.k8s.api.core.v1.RBDVolumeSource",
                        "io.k8s.api.core.v1.ScaleIOVolumeSource",
                        "io.k8s.api.core.v1.SecretVolumeSource",
                        "io.k8s.api.core.v1.StorageOSVolumeSource",
                    }
                },
                new AddBaseClassTweak() {
                    BaseClassName = "EitherVolumeSource",
                    BaseClassDescription = "Base for Volume Sources that can serve as either persistent or non-persistent Volume Sources",
                    PromoteIncomingAssociations = true,
                    DerviedQualifiedNames = new string[] {
                        "io.k8s.api.core.v1.AWSElasticBlockStoreVolumeSource",
                        "io.k8s.api.core.v1.AzureDiskVolumeSource",
                        "io.k8s.api.core.v1.FCVolumeSource",
                        "io.k8s.api.core.v1.FlockerVolumeSource",
                        "io.k8s.api.core.v1.HostPathVolumeSource",
                        "io.k8s.api.core.v1.NFSVolumeSource",
                        "io.k8s.api.core.v1.PhotonPersistentDiskVolumeSource",
                        "io.k8s.api.core.v1.PortworxVolumeSource",
                        "io.k8s.api.core.v1.QuobyteVolumeSource",
                        "io.k8s.api.core.v1.VsphereVirtualDiskVolumeSource",
                    }
                },
            };
            source.PostHydrationTweaks = new List<Tweak>() {
                new K8sTocTweak(),
                new MoveDerivedToPeerLevel() {
                    BaseClassName = "io.k8s.api.core.v1.PersistentVolumeSource",
                },
                new MoveDerivedToPeerLevel() {
                    BaseClassName = "io.k8s.api.core.v1.VolumeSource",
                },
                new MoveDerivedToPeerLevel() {
                    BaseClassName = "io.k8s.api.core.v1.EitherVolumeSource",
                },
            };

            Schema schema = Schema.CreateSchema(source);

            schema.BoringProperties = new string[] {
                "apiVersion", "kind"
            };

            GenerateGraphsAndDataDictionary();
        }

        private static void GenerateGraphsAndDataDictionary() {
            // Parse "visualizations.yaml" files
            List<GraphDefinition> graphDefsFromMetadata = new List<GraphDefinition>();
            // TODO: Note that we've temporarily lost this feature - must decide where to keep this.
            ApplyGraphDefsToSchema(graphDefsFromMetadata);

            // Copy static assets to output directory
            DirUtils.CopyDirRecursively(Path.Combine(Env.REPO_ROOT, "assets"),
                                        Path.Combine(Env.OUTPUT_ROOT_DIR, "assets"));

            // Graphviz MUST have access to images at the exact same path as it must ultimately
            // generate in the svg file. Hence this unsavory solution...
            DirUtils.CopyDirRecursively(Path.Combine(Env.REPO_ROOT, "assets"), "/assets");

            HierarchyItem topLevel = HierarchyItem.CreateHierarchyTree();
            HierarchyItemInfo.AssignColors(topLevel);
            GraphGenerator.CreateGraphDefinitions(topLevel);
            GraphGenerator.Generate(topLevel, graphDefsFromMetadata);

            // Since the SVG index is ***embedded*** within the HTML index file,
            // it must be generated first
            GraphvizIndexGenerator.GenerateIndex(topLevel);
            HtmlIndexGenerator.GenerateIndex(Env.OUTPUT_ROOT_DIR, topLevel);

            DataDictionaryGenerator.Generate(Env.OUTPUT_ROOT_DIR, Schema.Singleton.Models);
        }

        // TODO: This is currently dead code
        private static void ApplyGraphDefsToSchema(List<GraphDefinition> graphDefs) {
            foreach (GraphDefinition graphDef in graphDefs) {
                string[] nameComponents = graphDef.NameComponents;
                    foreach (Model model in graphDef.CoreModels) 
                        model.Levels = graphDef.NameComponents;
            }
        }
    }
}
