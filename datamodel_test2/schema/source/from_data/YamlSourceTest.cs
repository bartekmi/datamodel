using System;
using System.Text.RegularExpressions;

using Xunit;
using Xunit.Abstractions;

using Newtonsoft.Json;

namespace datamodel.schema.source.from_data {
    public class YamlSourceTest {

        private readonly ITestOutputHelper _output;

        public YamlSourceTest(ITestOutputHelper output) {
            _output = output;
        }

        [Fact]
        public void Read() {
            Env.Configure();
            TextSource text = TextSource.Text(@"a:
  sub:
  - name: 12
    clust: false
    labels:
      some_label: my_label
    decorate: true
    spec:
      containers:
      - image: 12.5
        command:
        - c1
        - c2
");
            YamlSource source = new YamlSource(text);

            string json = JsonConvert.SerializeObject(source._source, Formatting.Indented);
            _output.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> RESULTS >>>>>>>>>>>>>>>>>>>>");
            _output.WriteLine(json);

            // Uncomment this line to see the results of the output above
            // Assert.False(true, "Fail on purpose");
        }
    }
}