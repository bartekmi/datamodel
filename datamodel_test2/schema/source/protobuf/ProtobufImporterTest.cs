using System;
using System.Linq;
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
            RunTest("msgA,msgB1,nestedB", "test1", "a.proto");
        }

        [Fact]
        public void DoubleImport() {
            RunTest("msgA,msgB1,msgB2,msgC1,msgC2", "test2", "a.proto");
        }

        [Fact]
        public void ServiceImport() {
            RunTest("request,response", "test3", "a.proto");
        }

        #region Utilities
        private void RunTest(string expectedMessages, string basePath, string protoFilePath) {
            string actualMessages = ReadBundle(basePath, protoFilePath, out FileBundle bundle);

            if (actualMessages != expectedMessages) {
                string bundleJson = JsonFormattingUtils.JsonPretty(bundle);

                _output.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
                _output.WriteLine(bundleJson);      // We do this to get actual in full glory
                _output.WriteLine("<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");

                Assert.Equal(expectedMessages, actualMessages);
            }
        }

        private string ReadBundle(string basePath, string protoFilePath, out FileBundle bundle) {
            string baseBasePath = Path.Join("../../../schema/source/protobuf", basePath);
            ProtobufImporter importer = new ProtobufImporter(baseBasePath);

            string path = Path.Join(baseBasePath, protoFilePath);
            bundle = importer.ProcessFile(PathAndContent.Read(path));

            return string.Join(",", bundle.AllMessages.Select(x => x.Name));
        }
        #endregion
   }
}