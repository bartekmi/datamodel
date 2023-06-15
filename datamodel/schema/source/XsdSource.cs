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
                // Complex Types which contain <xsd:sequence> which contains a single (repeating) element
                // should not trigger the creation of a model - basically List<T>, since this contributes 
                // nothing to visualization. All we want is to create a 1:n association.
                if (cplxType.Particle is XmlSchemaSequence seq && seq.Items.Count == 1 && seq.Items[0] is XmlSchemaElement seqElement)
                    AddPropertyOrAssociation(parentModel, seqElement, element.Name);
                else
                    ParseComplexType(parentModel, element, cplxType);
            else if (element.SchemaType is XmlSchemaSimpleType simpleType)
                AddProperty(parentModel, element, null);
            else if (element.SchemaType == null)
                AddPropertyOrAssociation(parentModel, element, element.Name);
            else
                throw new NotImplementedException("Not sure when we'd ever land here");
        }

        private void AddPropertyOrAssociation(Model parentModel, XmlSchemaElement element, string roleName) {
                string otherSideType = element.SchemaTypeName.Name;
                bool isAssoc = _types.TryGetValue(otherSideType, out XmlSchemaType type)
                    && type is XmlSchemaComplexType;

                // Nested object referenced by type name - Add Association.
                if (isAssoc)
                    AddAssociation(parentModel, element, otherSideType, roleName);
                else
                    AddProperty(parentModel, element, otherSideType);
        }

        private void AddAssociation(Model ownerModel, XmlSchemaElement element, string otherSide, string otherRole) {
            _associations.Add(new Association() {
                OwnerSide = ownerModel.QualifiedName,
                OwnerMultiplicity = Multiplicity.Aggregation,
                OtherSide = otherSide,
                OtherMultiplicity = GetOtherMultiplicity(element),
                OtherRole = otherRole,
            });
        }

        private static Multiplicity GetOtherMultiplicity(XmlSchemaParticle particle) {
            if (particle.MaxOccursString?.ToLower() == "unbounded")
                return Multiplicity.Many;
            return particle.MinOccurs == 0 ? Multiplicity.ZeroOrOne : Multiplicity.One;
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

            // Nested object specified inline - Add Association.
            if (ownerModel != null)
                AddAssociation(ownerModel, parent, model.QualifiedName, parent.Name);

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

        private static void AddProperty(Model model, XmlSchemaElement element, string dataType) {
            if (element.MaxOccursString == "unbounded")
                dataType = string.Format("[]{0}", dataType);

            model.AllProperties.Add(new Property() {
                Name = element.Name,
                DataType = string.IsNullOrEmpty(dataType) ? "string" : dataType,
                CanBeEmpty = element.MinOccurs == 0,
                Description = ExtractDescription(element),
            });
        }

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

        // private Enum GetEnum(Dictionary<string, string> sEnum) {
        //     if (sEnum == null)
        //         return null;

        //     Enum theEnum = new Enum();
            
        //     foreach (var entry in sEnum)
        //         theEnum.Add(entry.Key, entry.Value);

        //     return theEnum;
        // }

        public override IEnumerable<Association> GetAssociations() {
            return _associations;
        }
    }
}
