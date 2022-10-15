using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;

namespace datamodel.schema.source {
    public class SimpleSource : SchemaSource {
        private SSchema _schema;

        private const string PARAM_FILE = "file";

        public override void Initialize(Parameters parameters) {
            string json = parameters.GetFileContent(PARAM_FILE);
            _schema = JsonConvert.DeserializeObject<SSchema>(json);
        }

        public override IEnumerable<Parameter> GetParameters() {
            return new List<Parameter>() {
                new Parameter() {
                    Name = PARAM_FILE,
                    Description = "The name of the file which contains the SimpleSource JSON",
                    Type = ParamType.File,
                    IsMandatory = true,
                }
            };
        }

        public override string GetTitle() {
            return _schema.Title;
        }

        public override IEnumerable<Model> GetModels() {
            List<Model> models = new List<Model>();
            if (_schema.Models == null)
                return models;

            foreach (SModel sModel in _schema.Models) {
                Model model = new Model() {
                    Name = sModel.Name,
                    QualifiedName = sModel.Name,
                    Description = sModel.Description,
                    IsAbstract = sModel.IsAbstract,
                    SuperClassName = sModel.SuperClass,
                    Levels = sModel.Levels,
                };
                model.AllProperties = GetProperties(sModel.Properties);
                models.Add(model);
            }

            return models;
        }

        private List<Property> GetProperties(List<SProperty> sProperties) {
            List<Property> properties = new List<Property>();
            if (sProperties == null)
                return properties;

            foreach (SProperty sProp in sProperties) {
                Property property = new Property() {
                    Name = sProp.Name,
                    Description = sProp.Description,
                    DataType = sProp.Type,
                    CanBeEmpty = sProp.CanBeEmpty,
                    Enum = GetEnum(sProp.Enum),
                };
                
                if (property.DataType == null && property.Enum != null)
                    property.DataType = "Enum";

                properties.Add(property);
            }

            return properties;
        }

        private Enum GetEnum(Dictionary<string, string> sEnum) {
            if (sEnum == null)
                return null;

            Enum theEnum = new Enum();
            
            foreach (var entry in sEnum)
                theEnum.Add(entry.Key, entry.Value);

            return theEnum;
        }

        public override IEnumerable<Association> GetAssociations() {
            List<Association> associations = new List<Association>();
            if (_schema.Associations == null)
                return associations;

            foreach (SAssociation sAssoc in _schema.Associations) {
                Association assoc = new Association() {
                    OwnerSide = sAssoc.A_Model,
                    OwnerMultiplicity = sAssoc.A_Card,
                    OwnerRole = sAssoc.A_Role,

                    OtherSide = sAssoc.B_Model,
                    OtherMultiplicity = sAssoc.B_Card,
                    OtherRole = sAssoc.B_Role,

                    Description = sAssoc.Description,
                };
                associations.Add(assoc);
            }

            return associations;
        }

    }

    #region Json Parse Classes
    public class SSchema {
        public string Title;
        public List<SModel> Models;
        public List<SAssociation> Associations;
    }

    public class SModel {
        public List<SProperty> Properties;

        public string Name;
        public string Description;
        public bool IsDeprecated;

        public string SuperClass;
        public bool IsAbstract;

        public string[] Levels;
    }

    public class SProperty {
        public string Name;
        public string Description;
        public bool IsDeprecated;
        public bool CanBeEmpty;

        public string Type;
        public Dictionary<string, string> Enum;
    }

    public class SAssociation {
        public bool IsDeprecated;
        public string Description;

        public string A_Model;
        public Multiplicity A_Card;
        public string A_Role;

        public string B_Model;
        public Multiplicity B_Card;
        public string B_Role;
    }
    #endregion
}
