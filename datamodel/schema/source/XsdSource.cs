using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Linq;

using datamodel.schema.tweaks;

namespace datamodel.schema.source {
    public class XsdSource : SchemaSource {

        private const string PARAM_URL = "url";
        private const string PARAM_DROP_MODEL_SUFFIX = "dropsuffix";

        private XmlSchema _schema;
        private Dictionary<string, XmlSchemaType> _types;
        private List<Model> _models = new List<Model>();
        private List<Association> _associations = new List<Association>();

        public override void Initialize(Parameters parameters) {
            string urlData = parameters.GetUrlContent(PARAM_URL);
            using TextReader reader = new StringReader(urlData);
            _schema = XmlSchema.Read(reader, null);

            List<Model> models = new List<Model>();

            XmlSchemaElement rootElement = _schema.Items.OfType<XmlSchemaElement>().Single();
            _types = _schema.Items.OfType<XmlSchemaType>()
                .ToDictionary(x => x.Name);

            foreach (XmlSchemaComplexType cplxType in _types.Values.OfType<XmlSchemaComplexType>())
                ParseComplexType(null, null, cplxType);

            ParseElement(null, rootElement);    

            // If required rename models to drop a particular suffix
            string dropModelSuffix = parameters.GetString(PARAM_DROP_MODEL_SUFFIX);
            if  (dropModelSuffix != null)
                Tweaks.Add(new RenameModelTweak() {
                    SuffixToRemove = dropModelSuffix,
                });
        }

        private void ParseElement(Model parentModel, XmlSchemaElement element) {
            if (element.SchemaType is XmlSchemaComplexType cplxType)
                // Complex Types which contain <xsd:sequence> which contains a single repeating element
                // should not trigger the creation of a model - They basically represent List<T>.
                // Creating models for these contributes nothing to visualization. All we want is to create a 1:n association.
                if (cplxType.Particle is XmlSchemaSequence seq && seq.Items.Count == 1 && seq.Items[0] is XmlSchemaElement seqElement)
                    AddPropertyOrAssociation(parentModel, element, seqElement);
                else
                    ParseComplexType(parentModel, element, cplxType);
            else if (element.SchemaType is XmlSchemaSimpleType simpleType)
                AddProperty(parentModel, element, element, simpleType);
            else if (element.SchemaType == null)
                AddPropertyOrAssociation(parentModel, element, element);
            else
                throw new NotImplementedException("Not sure when we'd ever land here");
        }

        private void AddPropertyOrAssociation(Model parentModel, XmlSchemaElement forName, XmlSchemaElement forMultiplicity) {
                string otherSideType = forMultiplicity.SchemaTypeName.Name;
                _types.TryGetValue(otherSideType, out XmlSchemaType type);

                // Nested object referenced by type name - Add Association.
                if (type is XmlSchemaComplexType)
                    AddAssociation(parentModel, forName, forMultiplicity, otherSideType);
                else {
                    AddProperty(parentModel, forName, forMultiplicity, type as XmlSchemaSimpleType);
                }
        }

        private void ParseComplexType(Model ownerModel, XmlSchemaElement parent, XmlSchemaComplexType cplxType) {
            // Create the Model
            string name = parent == null ? cplxType.Name : parent.Name;
            string description = ExtractDescription(parent == null ? cplxType : parent);

            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("Blank Complex Type name at line " + cplxType.LineNumber);

            Model model = new Model() {
                Name = name,
                QualifiedName = GetQualifiedName(cplxType),
                Description = description,
                IsAbstract = cplxType.IsAbstract,
                // Levels = ???,
            };

            // If the nestsed Particle is a Choice - tag the model to state that at most ONE of the child properties or associations
            // can be present
            if (cplxType.Particle is XmlSchemaChoice) {
                model.AddLabel("NOTE", "This element is an xsd:choice node, meaning that at most ONE of the child properties or child associations may be present.");
                model.ColorStringOverride = "greenyellow";
            }

            // Nested object specified inline - Add Association.
            if (ownerModel != null)
                AddAssociation(ownerModel, parent, parent, model.QualifiedName);

            // Add Properties
            XmlSchemaGroupBase group = cplxType.Particle as XmlSchemaGroupBase;
            if (group == null)
                throw new Exception("Null group at line " + cplxType.LineNumber);

            foreach (XmlSchemaObject child in group.Items) {
                if (child is XmlSchemaSimpleType simpleType) {
                    throw new NotImplementedException();
                } else if (child is XmlSchemaComplexType childCplxType) {
                    throw new NotImplementedException();
                } else if (child is XmlSchemaElement childElement) {
                    ParseElement(model, childElement);
                }
            }

            _models.Add(model);
        }

        #region Utilities

        private static string GetQualifiedName(XmlSchemaObject obj) {
            List<string> parts = new List<string>();

            while (obj != null) {
                if (obj is XmlSchemaElement element)
                    parts.Add(element.Name);
                else if (obj is XmlSchemaType type && !string.IsNullOrEmpty(type.Name))
                    parts.Add(type.Name);
                obj = obj.Parent;
            }

            parts.Reverse();
            return string.Join(".", parts);
        }

        private static string ExtractDescription(XmlSchemaAnnotated annotated) {
            XmlSchemaDocumentation doc = annotated?.Annotation?.Items.OfType<XmlSchemaDocumentation>().SingleOrDefault();
            return doc == null ? null : doc.Markup?.SingleOrDefault()?.InnerText;
        }

        private static void AddProperty(Model model, XmlSchemaElement forName, XmlSchemaElement forMultiplicity, XmlSchemaSimpleType simpleType) {
            // Determine Data Type
            Enum enumeration = MaybeCreateEnum(simpleType);
            string finalType = simpleType?.Name ?? forName.SchemaTypeName?.Name;
            if (string.IsNullOrEmpty(finalType))
                finalType = enumeration == null ? "string" : "enum"; 
            if (IsMultiple(forMultiplicity))
                finalType = string.Format("[]{0}", finalType);

            // Create the property
            model.AllProperties.Add(new Property() {
                Name = forName.Name,
                DataType = finalType,
                CanBeEmpty = forName.MinOccurs == 0,
                Description = ExtractDescription(forMultiplicity),
                Enum = enumeration,
            });
        }

        private static Enum MaybeCreateEnum(XmlSchemaSimpleType simpleType) {
            if (simpleType?.Content is XmlSchemaSimpleTypeRestriction restr) {
                Enum enumeration = new Enum() {
                    Name = simpleType.Name,
                };

                foreach (var item in restr.Facets.OfType<XmlSchemaEnumerationFacet>()) 
                    enumeration.Add(item.Value, null);

                if (enumeration.Values.Count() > 0)
                    return enumeration;
            }

            return null;
        }

        private void AddAssociation(Model ownerModel, XmlSchemaElement forName, XmlSchemaElement forMultiplicity, string otherSide) {
            _associations.Add(new Association() {
                OwnerSide = ownerModel.QualifiedName,
                OwnerMultiplicity = Multiplicity.Aggregation,
                OtherSide = otherSide,
                OtherMultiplicity = GetOtherMultiplicity(forMultiplicity),
                OtherRole = forName.Name,
                Description = ExtractDescription(forName),
            });
        }

        private static Multiplicity GetOtherMultiplicity(XmlSchemaParticle particle) {
            if (IsMultiple(particle))
                return Multiplicity.Many;
            return particle.MinOccurs == 0 ? Multiplicity.ZeroOrOne : Multiplicity.One;
        }

        private static bool IsMultiple(XmlSchemaParticle particle) {
            return particle.MaxOccursString?.ToLower() == "unbounded"
                || particle.MaxOccurs > 1;
        }
        #endregion

        #region Abstract Class Implementation

        public override IEnumerable<Parameter> GetParameters() {
            return new List<Parameter>() {
                new Parameter() {
                    Name = PARAM_URL,
                    Description = "The URL from which the XSD file can be downloaded",
                    Type = ParamType.Url,
                    IsMandatory = true,
                },
                new Parameter() {
                    Name = PARAM_DROP_MODEL_SUFFIX,
                    Description = "If specified, this suffix will be dropped from model names",
                    Type = ParamType.String,
                }
            };
        }

        public override string GetTitle() {
            return _schema.TargetNamespace;
        }

        public override IEnumerable<Model> GetModels() {
            return _models;
        }

        public override IEnumerable<Association> GetAssociations() {
            return _associations;
        }
        #endregion
    }
}
