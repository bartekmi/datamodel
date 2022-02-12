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

        public override IEnumerable<Model> GetModels() {
            List<Model> models = new List<Model>();
            if (_schema.Models == null)
                return models;

            foreach (SModel sModel in _schema.Models) {
                Model model = new Model() {
                    Name = sModel.Name,
                    Description = sModel.Description,
                    IsAbstract = sModel.IsAbstract,
                    SuperClassName = sModel.SuperClass,

                    Team = sModel.Level1,
                    Engine = sModel.Level2,
                    ModuleOverride = sModel.Level3,
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
                };
                columns.Add(column);
            }

            return columns;
        }

        public override IEnumerable<Association> GetAssociations() {
            List<Association> associations = new List<Association>();
            if (_schema.Associations == null)
                return associations;

            foreach (SAssociation sAssoc in _schema.Associations) {
                Association assoc = new Association() {
                    FkSide = sAssoc.A_Model,
                    FkSideMultiplicity = sAssoc.A_Card,
                    RoleByFK = sAssoc.A_Role,

                    OtherSide = sAssoc.B_Model,
                    OtherSideMultiplicity = sAssoc.B_Card,
                    RoleOppositeFK = sAssoc.B_Role,
                };
                associations.Add(assoc);
            }

            return associations;
        }

    }

    public class SSchema {
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

        public DataType Type;
        public bool IsNull;
    }

    public class SAssociation {
        public bool IsDeprecated;

        public string A_Model;
        public Multiplicity A_Card;
        public string A_Role;

        public string B_Model;
        public Multiplicity B_Card;
        public string B_Role;
    }

    public class SEnum {
        public List<string> Values;
    }
}
