using System;
using System.IO;
using System.Collections.Generic;

using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json;

namespace datamodel.schema.source {
    public class SwaggerSourceTest {

        private readonly ITestOutputHelper _output;

        public SwaggerSourceTest(ITestOutputHelper output) {
            _output = output;
        }

        [Fact]
        public void Read() {
            SwaggerSource source = SwaggerSource.FromFile("../../../schema/swagger_schema.json",
                new SwaggerSourceOptions() {
                    BoringNameComponents = new string[] {
                        "io", "k8s", "v1", "api"
                    }
                });

            Schema.CreateSchema(source);

            var output = new ModelsAndReferences() {
                Title = source.GetTitle(),
                Models = source.GetModels(),
                Associations = source.GetAssociations(),
            };

            string json = JsonConvert.SerializeObject(output, Formatting.Indented);
            _output.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> RESULTS >>>>>>>>>>>>>>>>>>>>");
            _output.WriteLine(json);

            // Uncomment this line to see the results of the output above
            Assert.False(true, "Fail on purpose");
        }
    }

    public class ModelsAndReferences {
        public string Title;
        public IEnumerable<Model> Models;
        public IEnumerable<Association> Associations;
    }
}