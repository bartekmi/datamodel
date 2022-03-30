using System;
using System.Text.RegularExpressions;

using Xunit;
using Xunit.Abstractions;

using Newtonsoft.Json;

namespace datamodel.schema.source {
    public class JsonSourceTest {

        private readonly ITestOutputHelper _output;

        public JsonSourceTest(ITestOutputHelper output) {
            _output = output;
        }

        [Fact]
        public void Read() {
            Env.Configure();
            
            JsonSource source = new JsonSource("../../../schema/json_source.json", 
                new JsonSource.Options() {
                    RootObjectName = "kubernetes",
                    PathsWhereKeyIsData = new string[] {
                        "root.key_is_value",
                    }
                }
            );

            string json = JsonConvert.SerializeObject(source._source, Formatting.Indented);
            _output.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> RESULTS >>>>>>>>>>>>>>>>>>>>");
            _output.WriteLine(json);

            // Uncomment this line to see the results of the output above
            Assert.False(true, "Fail on purpose");
        }

        [Fact]
        public void RegexMatch() {
            bool isMatch = SampleDataSchemaSource.PROP_NAME_REGEX.IsMatch("definitions");
            Assert.True(isMatch);
        }
    }
}