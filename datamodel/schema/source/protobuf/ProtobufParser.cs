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
                    } else if (PeekAndDiscard("extend")) {
                        Extend extend = ParseExtend();
                        file.Extends.Add(extend);
                    } else if (PeekAndDiscard("service")) {
                        Service service = ParseService();
                        file.Services.Add(service);
                    } else if (PeekAndDiscard(";")) {
                        // Do nothing - emptyStatement
                    } else
                        throw new Exception("Unexpected token: " + Next());
                }
            } catch (Exception e) {
                string message = string.Format("Parse error on line {0}.", _tokenizer.LineNumber);
                throw new Exception(message, e);
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
            file.Imports.Add(new Import() {
                ImportPath = path,
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

            message.Fields.AddRange(ParseMessageBody(message));

            return message;
        }

        private List<Field> ParseMessageBody(Message message) {
            List<Field> fields = new List<Field>();

            Expect("{");

            while (!PeekAndDiscard("}")) {
                if (PeekAndDiscard("option"))
                    ParseOption();
                else if (PeekAndDiscard("reserved"))
                    ParseReserved();
                else if (PeekAndDiscard("extensions"))          // Protobuf 2
                    ParseExtension();
                else if (PeekAndDiscard("message")) {
                    Message nested = ParseMessage(message);
                    message.Messages.Add(nested);
                } else if (PeekAndDiscard("enum")) {
                    EnumDef theEnum = ParseEnumDefinition(message);
                    message.EnumDefs.Add(theEnum);
                } else if (PeekAndDiscard("extend")) {
                    Extend extend = ParseExtend();
                    message.Extends.Add(extend);
                } else if (PeekAndDiscard(";")) {
                    // Do nothing - emptyStatement
                } else if (PeekAndDiscard("oneof")) {
                    FieldOneOf oneOf = ParseOneOfField(message);
                    fields.Add(oneOf);
                } else if (PeekAndDiscard("map")) {
                    FieldMap map = ParseMapField(message);
                    fields.Add(map);
                } else {
                    Field normal = ParseNormalOrGroupField(message);
                    fields.Add(normal);
                }
            }

            return fields;
        }

        // Example:
        //  oneof foo {
        //      string name = 4 [...options...];
        //      SubMessage sub_message = 9;
        //  }
        private FieldOneOf ParseOneOfField(Message owner) {
            FieldOneOf field = new FieldOneOf(owner) {
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
                    string type = Next();
                    FieldNormal normal = ParseNormalField(owner, FieldModifier.None, type);
                    field.Fields.Add(normal);
                }
            }

            return field;
        }

        // Example:
        //  map<string, Project> projects = 3 [...options...];
        private FieldMap ParseMapField(Message owner) {
            FieldMap map = new FieldMap(owner) {
                Comment = CurrentComment(),
            };
            Expect("<");
            map.KeyType = new Type(map, Next());
            Expect(",");
            map.ValueType = new Type(map, Next());
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
        private Field ParseNormalOrGroupField(Message message) {
            string groupOrType = Next();
            FieldModifier modifier = FieldModifier.None;

            // Parse optional modifier
            switch (groupOrType) {
                case "required":
                    modifier = FieldModifier.Required;
                    break;
                case "optional":
                    modifier = FieldModifier.Optional;
                    break;
                case "repeated":
                    modifier = FieldModifier.Repeated;
                    break;
            }
            if (modifier != FieldModifier.None)
                groupOrType = Next();

            // Parse either a normal or gropu field
            if (groupOrType == "group")     // Proto2. Could explicitly check syntax first.
                return ParseGroupField(message, modifier);
            else 
                return ParseNormalField(message, modifier, groupOrType);
        }

        private FieldNormal ParseNormalField(Message owner, FieldModifier modifier, string type) {
            // Order of populating fields is important
            FieldNormal field = new FieldNormal(owner) {
                Modifier = modifier,
                Comment = CurrentComment(),
            };
            field.Type = new Type(field, type);
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

        // service SearchService {
        //   rpc Search (SearchRequest) returns (SearchResponse);           // Alternative #1
        //   rpc Search2 (MyRequest) returns (MyResponse) {...options...}   // Alternative #2
        // }
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

            // Note the two alternate endings
            if (PeekAndDiscard(";")) {              // Alternative #1
                // We're done
            } else {                                // Alternative #2
                Expect("{");
                while (!PeekAndDiscard("}")) {
                    if (PeekAndDiscard(";")) {
                        // Do nothing - emptyStatement
                    } else if (PeekAndDiscard("option"))
                        ParseOption();
                    else
                        throw new Exception("Expecting option or ;");
                }
            }

            return rpc;
        }
        #endregion

        #region Protobuf 2 Specific

        private void ParseExtension() {
            // For now, we will discard 
            string next;
            while ((next = Next()) != ";");
        }

        // repeated group Result = 1 {
        //     required string url = 2;
        //     optional string title = 3;
        //     repeated string snippets = 4;
        // }
        private FieldGroup ParseGroupField(Message message, FieldModifier modifier) {
            // Order of populating fields is important
            FieldGroup field = new FieldGroup(message) {
                Comment = CurrentComment(),
                Name = Next(),
            };

            Expect("=");
            field.Number = ParseInt();

            field.Fields.AddRange(ParseMessageBody(message));

            return field;
        }

        private Extend ParseExtend() {
            // Order of populating fields is important
            Extend extend = new Extend() {
                Comment = CurrentComment(),
                MessageType = Next(),
            };
            Expect("{");

            while (!PeekAndDiscard("}")) {
                if (PeekAndDiscard(";")) {
                    // Do nothing - emptyStatement
                } else
                    extend.Fields.Add(ParseNormalOrGroupField(extend.Message));
            }

            return extend;
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
