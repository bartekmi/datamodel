// The ProtobufParser class performs the first stage of the parsing of Protobuf files
// - creating the AST, but with no attempt to connect the various enums and messages
// to data types.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace datamodel.schema.source.protobuf {
    internal class ParseContext {
        internal Dictionary<string, File> AllFiles = new Dictionary<string, File>();
    }

    internal class ProtobufParser {

        private ProtobufTokenizer _tokenizer;

        internal ProtobufParser(ProtobufTokenizer tokenizer) {
            _tokenizer = tokenizer;
        }

        #region Main Parse
        internal File Parse() {
            File file = new File();

            try {
                while (_tokenizer.HasNext()) {
                    if (PeekAndDiscard("syntax"))
                        ParseSyntax(file);
                    else if (PeekAndDiscard("package"))
                        ParsePackage(file);
                    else if (PeekAndDiscard("import"))
                        ParseImport(file);
                    else if (PeekAndDiscard("option"))
                        ParseOption();
                    else if (PeekAndDiscard("message")) {
                        Message message = ParseMessage(file);
                        file.Messages.Add(message);
                    } else if (PeekAndDiscard("enum")) {
                        EnumDef theEnum = ParseEnumDefinition(file);
                        file.EnumDefs.Add(theEnum);
                    } else if (PeekAndDiscard("service")) {
                        Service service = ParseService();
                        file.Services.Add(service);
                    } else if (PeekAndDiscard(";")) {
                        // Do nothing - emptyStatement
                    } else
                        throw new Exception("Unexpected token: " + Next());
                }
            } catch (Exception e) {
                throw new Exception("Parse error on line " + _tokenizer.LineNumber, e);
            }

            return file;
        }
        #endregion

        #region Various Parse Methods
        private void ParseSyntax(File file) {
            Expect("=");
            file.Syntax = Next();
            Expect(";");
        }

        private void ParsePackage(File file) {
            file.Package = Next();
            Expect(";");
        }

        private void ParseImport(File file) {
            string path = Next();
            
            ImportType importType = ImportType.None;
            if (path == "weak") {
                importType = ImportType.Weak;
                path = Next();
            } else if (path == "public") {
                importType = ImportType.Public;
                path = Next();
            }
            
            Expect(";");
            file.Imports.Add(new File() {
                Path = path,
                ImportType = importType,
            });
        }

        private void ParseOption() {
            // For now, we will discard any options
            string next;
            while ((next = Next()) != ";");
        }

        private void ParseOptions() {
            if (PeekAndDiscard("[")) {
                string next;
                while ((next = Next()) != "]");
            }
        }

        private Message ParseMessage(Owner owner) {
            Message message = new Message() {
                Comment = CurrentComment(),
                Name = Next(),
                Owner = owner,
            };

            Expect("{");

            while (!PeekAndDiscard("}")) {
                if (PeekAndDiscard("option"))
                    ParseOption();
                else if (PeekAndDiscard("reserved"))
                    ParseReserved();
                else if (PeekAndDiscard("message")) {
                    Message nested = ParseMessage(message);
                    message.Messages.Add(nested);
                } else if (PeekAndDiscard("enum")) {
                    EnumDef theEnum = ParseEnumDefinition(message);
                    message.EnumDefs.Add(theEnum);
                } else if (PeekAndDiscard(";")) {
                    // Do nothing - emptyStatement
                } else if (PeekAndDiscard("oneof")) {
                    FieldOneOf oneOf = ParseOneOfField();
                    message.Fields.Add(oneOf);
                } else if (PeekAndDiscard("map")) {
                    FieldMap map = ParseMapField();
                    message.Fields.Add(map);
                } else {
                    Field normal = ParseNormalField();
                    message.Fields.Add(normal);
                }
            }

            return message;
        }

        // Example:
        //  oneof foo {
        //      string name = 4 [...options...];
        //      SubMessage sub_message = 9;
        //  }
        private FieldOneOf ParseOneOfField() {
            FieldOneOf field = new FieldOneOf() {
                Comment = CurrentComment(),
                Name = Next(),
            };
            Expect("{");

            while (!PeekAndDiscard("}")) {
                if (PeekAndDiscard("option"))
                    ParseOption();
                else if (PeekAndDiscard(";")) {
                    // Do nothing - emptyStatement
                } else {
                    FieldNormal normal = ParseNormalField();
                    field.Fields.Add(normal);
                }
            }

            return field;
        }

        // Example:
        //  map<string, Project> projects = 3 [...options...];
        private FieldMap ParseMapField() {
            FieldMap map = new FieldMap() {
                Comment = CurrentComment(),
            };
            Expect("<");
            map.KeyType = new Type(Next());
            Expect(",");
            map.ValueType = new Type(Next());
            Expect(">");
            map.Name = Next();
            Expect("=");
            map.Number = ParseInt();
            ParseOptions();
            Expect(";");

            return map;
        }

        // Template: 
        //  [repeated] type name = n [ [...options...] ];
        private FieldNormal ParseNormalField() {
            FieldNormal field = new FieldNormal() {
                Comment = CurrentComment(),
            };
            string type = Next();
            if (type == "repeated") {
                field.Modifier = FieldModifier.Repeated;
                type = Next();
            }
            field.Type = new Type(type);

            field.Name = Next();
            Expect("=");
            field.Number = ParseInt();
            ParseOptions();
            return field;
        }
         
        private void ParseReserved() {
            // For now, we will discard reserved
            string next;
            while ((next = Next()) != ";");
        }

        private EnumDef ParseEnumDefinition(Owner owner) {
            EnumDef theEnum = new EnumDef() {
                Comment = CurrentComment(),
                Name = Next(),
                Owner = owner,
            };

            Expect("{");

            while (!PeekAndDiscard("}")) {
                if (PeekAndDiscard("option"))
                    ParseOption();
                else if (PeekAndDiscard(";")) {
                    // Do nothing - emptyStatement
                } else {     // Assume it's "item = n;"
                    EnumValue value = new EnumValue() {
                        Comment = CurrentComment(),
                        Name = Next(),
                    };
                    Expect("=");
                    value.Number = ParseInt();
                    ParseOptions();
                    theEnum.Values.Add(value);
                }
            }

            return theEnum;
        }

        private Service ParseService() {
            Service service = new Service() {
                Comment = CurrentComment(),
                Name = Next()
            };

            Expect("{");

            while (!PeekAndDiscard("}")) {
                if (PeekAndDiscard("option"))
                    ParseOption();
                else if (PeekAndDiscard("rpc")) {
                    Rpc rpc = ParseRpc();
                    service.Rpcs.Add(rpc);
                } else if (PeekAndDiscard(";")) {
                    // Do nothing - emptyStatement
                } else
                    throw new Exception("Unexpected token: " + Next());
            }

            return service;
        }

        private Rpc ParseRpc() {
            Rpc rpc = new Rpc() {
                Comment = CurrentComment(),
                Name = Next()
            };

            // Input
            Expect("(");
            if (PeekAndDiscard("stream"))
                rpc.IsInputStream = true;
            rpc.InputName = Next();
            Expect(")");

            // Output
            Expect("returns");
            Expect("(");
            if (PeekAndDiscard("stream"))
                rpc.IsOutputStream = true;
            rpc.OutputName = Next();
            Expect(")");

            return rpc;
        }
        #endregion

        #region Utils
        internal string Next() {
            return _tokenizer.Next();
        }

        private int ParseInt() {
            bool isNegative = false;
            string valueStr = Next();
            if (valueStr == "-") {
                isNegative = true;
                valueStr = Next();
            }

            int value = int.Parse(valueStr);
            if (isNegative)
                value = -value;

            return value;
        }

        private bool PeekAndDiscard(string possiblyExpected) {
            string token = _tokenizer.Peek().ToLower();
            if (token == possiblyExpected.ToLower()) {
                Next();
                return true;
            }

            return false;
        }

        private void Expect(string expected) {
            string token = Next();
            if (token != expected.ToLower())
                throw new Exception(string.Format("Expected '{0}' but got '{1}' on line {2}", 
                    expected, token, _tokenizer.LineNumber));
        }

        private string CurrentComment() {
            return _tokenizer.Comment;
        }
        #endregion
    }
}