using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

using Xunit;
using Xunit.Abstractions;

using datamodel.utils;

namespace datamodel.schema.source.protobuf {
    public class ProtobufImporterTest {

        private readonly ITestOutputHelper _output;

        public ProtobufImporterTest(ITestOutputHelper output) {
            _output = output;
        }

        [Fact]
        public void BasicTestSingleImport() {
            RunTest(@"
 {
   AllMessages: [
     {
       Name: msgA,
       Fields: [
         {
           Type: {
             Name: b.msgB1
           },
           Number: 1,
           Name: f1
         },
         {
           Type: {
             Name: b.msgB1.nestedB
           },
           Number: 2,
           Name: f2
         }
       ]
     },
     {
       Name: msgB1,
       Messages: [
         {
           Name: nestedB
         }
       ]
     },
     {
       Name: nestedB
     }
   ]
 }", "a.proto");
        }

        #region Utilities
        private void RunTest(string expected, string protoFilePath) {
            string actual = ReadBundle(protoFilePath);
            expected = JsonFormattingUtils.DeleteFirstSpace(expected);

            if (actual != expected) {
                _output.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
                _output.WriteLine(actual);      // We do this to get actual in full glory
                _output.WriteLine("<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");

                Assert.Equal(expected, actual);
            }
        }

        private string ReadBundle(string protoFilePath) {
            string basePath = "../../../schema/source/protobuf";
            ProtobufImporter importer = new ProtobufImporter(basePath);

            string path = Path.Join(basePath, protoFilePath);
            FileBundle bundle = importer.ProcessFile(PathAndContent.Read(path));
            bundle.RemoveComments();    // Clean up.

            return JsonFormattingUtils.JsonPretty(bundle);
        }
        #endregion
   }
}