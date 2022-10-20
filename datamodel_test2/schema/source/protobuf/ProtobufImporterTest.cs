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
        public void ReadFile() {
            string basePath = "../../../schema/source/protobuf";
            ProtobufImporter importer = new ProtobufImporter(basePath);

            string path = Path.Join(basePath, "a.proto");
            FileBundle bundle = importer.ProcessFile(PathAndContent.Read(path));

            _output.WriteLine("Printing bundle...");
            _output.WriteLine(JsonFormattingUtils.JsonPretty(bundle));

            Assert.Equal(2, bundle.PackageDict.Count);

            // Xunit only prints output on test failure
            // Assert.Equal(false, true);
        }
   }
}