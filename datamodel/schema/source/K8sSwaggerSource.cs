using System;
using System.Collections.Generic;
using System.Linq;

using datamodel.schema.tweaks;
using datamodel.utils;

namespace datamodel.schema.source {
    public class K8sSwaggerSource : SwaggerSource {
        public K8sSwaggerSource() {
            // While some of these tweaks are basically baked-in as part of
            // the Schema-Source (e.g. K8sTocTweak), some are more arbitrary,
            // and could be read in from a config file
            PreHydrationTweaks = new List<Tweak>() {
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
                new AddBaseClassTweak() {
                    BaseClassName = "AbstractMetricSource",
                    BaseClassDescription = "Base for Metric Sources",
                    PromoteIncomingAssociations = true,
                    DerviedQualifiedNames = new string[] {
                        "io.k8s.api.autoscaling.v2.ContainerResourceMetricSource",
                        "io.k8s.api.autoscaling.v2.ResourceMetricSource",
                        "io.k8s.api.autoscaling.v2.ExternalMetricSource",
                        "io.k8s.api.autoscaling.v2.PodsMetricSource",
                        "io.k8s.api.autoscaling.v2.ObjectMetricSource",
                    }
                },
                new AddBaseClassTweak() {
                    BaseClassName = "AbstractMetricStatus",
                    BaseClassDescription = "Base for Metric Statuses",
                    PromoteIncomingAssociations = true,
                    DerviedQualifiedNames = new string[] {
                        "io.k8s.api.autoscaling.v2.ContainerResourceMetricStatus",
                        "io.k8s.api.autoscaling.v2.ResourceMetricStatus",
                        "io.k8s.api.autoscaling.v2.ExternalMetricStatus",
                        "io.k8s.api.autoscaling.v2.PodsMetricStatus",
                        "io.k8s.api.autoscaling.v2.ObjectMetricStatus",
                    }
                },
                new AddBaseClassTweak() {
                    BaseClassName = "AbstractSecurityContext",
                    BaseClassDescription = "Base for Security Context and Pod Security Context",
                    PromoteIncomingAssociations = true,
                    DerviedQualifiedNames = new string[] {
                        "io.k8s.api.core.v1.SecurityContext",
                        "io.k8s.api.core.v1.PodSecurityContext",
                    }
                },
            };
            PostHydrationTweaks = new List<Tweak>() {
                new K8sTocTweak(),
                new MarkDeprecationsTweak(),
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
        }

        public override IEnumerable<Parameter> GetParameters() {
            IEnumerable<Parameter> parameters = base.GetParameters();

            parameters.Single(x => x.Name == SwaggerSource.PARAM_URL)
                .Default = "https://raw.githubusercontent.com/kubernetes/kubernetes/master/api/openapi-spec/swagger.json";

            parameters.Single(x => x.Name == SwaggerSource.PARAM_BORING_NAME_COMPONENTS)
                .Default = "io, k8s, api, pkg, v1, v1alpha1, v1beta1, v1beta2, v2, v2beta1, v2beta2";

            return parameters;
        }

        protected override void PopulateModel(Model model, string qualifiedName, SwgDefinition def) {
            base.PopulateModel(model, qualifiedName, def);

            // In almost all cases, the second-last element of the qualified name is
            // the API version of the entity. Set version info on the models.
            string[] pieces = qualifiedName.Split(".");
            if (pieces.Length >= 2) {
                string version = pieces.Reverse().Skip(1).First();
                if (version.ToLower().StartsWith("v")) {
                    model.Version = version;
                    var piecesLessVersion = pieces.Where(x => x != version);
                    model.QualifiedNameLessVersion = string.Join(".", piecesLessVersion);

                    model.AddLabel("Version", version);
                }
            }

            // Mark models which only serve as lists of other models
            Association assoc = _associations.SingleOrDefault(x => x.OwnerSide == model.QualifiedName && x.OtherRole == "items");
            if (model.Name.EndsWith("List") && assoc != null)
                model.ListSemanticsForType = assoc.OtherSide;

            // Populate enum definitions
            foreach (Column column in model.RegularColumns)
                if (column.Enum != null)
                    PopulateEnumDefinitions(model.HumanName, column);
        }

        private const string ENUMS_PATTERN = "\n\nPossible enum values:(\n - `.+`.*)+";
        private const string SINGLE_ENUM_PATTERN = "\n - `\"(.+)\"`(.*)$";

        internal static void PopulateEnumDefinitions(string modelName, Column column) {
            string name = string.Format("'{0}.{1}'", modelName, column.HumanName);
            string[] enums = RegExUtils.GetMultipleCaptures(column.Description, ENUMS_PATTERN);
            if (enums == null) {
                Error.Log("Could not extract enum values for field {0}", name);
                return;
            }

            bool hasError = false;
            Dictionary<string, string> descriptions = new Dictionary<string, string>();
            foreach (string pair in enums) {
                string[] enumAndDesc = RegExUtils.GetCaptureGroups(pair, SINGLE_ENUM_PATTERN);
                if (enumAndDesc == null || enumAndDesc.Length != 2) {
                    Error.Log("Could not extract enum description for field {0} from {1}", name, pair);
                    hasError = true;
                    continue;
                }

                descriptions[enumAndDesc[0]] = enumAndDesc[1].Trim();
            }

            foreach (var anEnum in column.Enum.Values) {
                if (!descriptions.TryGetValue(anEnum.Key, out string description)) {
                    Error.Log("Could not find enum description for field {0}, enum value {1}", name, anEnum.Key);
                    hasError = true;
                    continue;
                }

                column.Enum.SetDescription(anEnum.Key, description);
            }

            if (!hasError) {
                // Since we've successuflly extracted all enum descriptions, remove them from 
                // the Column description
                column.Description = RegExUtils.Replace(column.Description, ENUMS_PATTERN, "");
            }
        }
    }

    public class MarkDeprecationsTweak : Tweak {
        public override void Apply(TempSource source) {
            foreach (Model model in source.Models.Values) {
                SetDeprecatedIfNeeded(model);
                model.AllColumns.ForEach(x => SetDeprecatedIfNeeded(x));
            }
        }

        private static void SetDeprecatedIfNeeded(IDbElement element) {
            if (element.Name.ToLower().StartsWith("deprecated")) {
                element.Deprecated = true;
                return;
            }

            if (element.Description != null) {
                string lower = element.Description.ToLower();
                element.Deprecated = lower.Contains("deprecated.") || lower.Contains("deprecated:");
            }
        }
    }

    public class FilterOldApiVersionsTweak : FilterModelsTweak {
        public override IEnumerable<Model> ModelsToFilterOut(TempSource source) {
            List<Model> toRemove = new List<Model>();

            // Only take the latest of multiple versioned models 
            foreach (var group in source.GetModels().GroupBy(x => x.QualifiedNameLessVersion)) {
                if (group.Key != null && group.Count() > 1) {
                    var byVersion = group.OrderBy(x => x.Version, new VersionComparer());
                    toRemove.AddRange(byVersion.Take(byVersion.Count() - 1));
                }
            }

            return toRemove;
        }

        internal class VersionComparer : IComparer<string> {
            public int Compare(string a, string b) {
                int aInt = VersionToInt(a);
                int bInt = VersionToInt(b);

                return aInt.CompareTo(bInt);
            }

            // Format is expected to be one of:
            // vX
            // vXbetaY
            // vXalphaY
            private int VersionToInt(string version) {
                int versionContribution = (version[1] - '0') * 1000;
                int alphaBetaContribution = version.Length == 2 ?
                    999 : (version[2] - 'a') * 100;
                int minorVersionContribution = version.Last() - '0';

                return
                    versionContribution +
                    alphaBetaContribution +
                    minorVersionContribution;
            }
        }
    }
}