using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;

namespace datamodel.schema.source {
    public class SimpleSource : SchemaSource {
        private SSchema _schema;

        public SimpleSource(string filename) {
            string json = File.ReadAllText(filename);
            _schema = JsonConvert.DeserializeObject<SSchema>(json);
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
                    FullyQualifiedName = sModel.Name,
                    Description = sModel.Description,
                    IsAbstract = sModel.IsAbstract,
                    SuperClassName = sModel.SuperClass,

                    Level1 = sModel.Level1,
                    Level2 = sModel.Level2,
                    Level3 = sModel.Level3,
                };
                model.AllColumns = GetColumns(model, sModel.Properties);
                models.Add(model);
            }

            return models;
        }

        private List<Column> GetColumns(Model model, List<SProperty> properties) {
            List<Column> columns = new List<Column>();
            if (properties == null)
                return columns;

            foreach (SProperty sProp in properties) {
                // TODO: Better to set owner in Schema code
                Column column = new Column(model) {
                    Name = sProp.Name,
                    Description = sProp.Description,
                    DataType = sProp.Type,
                    CanBeEmpty = sProp.CanBeEmpty,
                    Enum = GetEnum(sProp.Enum),
                };
                
                if (column.DataType == null && column.Enum != null)
                    column.DataType = "Enum";

                columns.Add(column);
            }

            return columns;
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
                    FkSide = sAssoc.A_Model,
                    FkMultiplicity = sAssoc.A_Card,
                    FkRole = sAssoc.A_Role,

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

        public string Level1;
        public string Level2;
        public string Level3;
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
