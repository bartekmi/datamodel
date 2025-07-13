
using System.IO;
using datamodel.utils;
using Xunit;
using Xunit.Abstractions;

namespace datamodel.schema.source.plantuml;

public class PlantUmlSourceTest {
    private readonly ITestOutputHelper _output;

    public PlantUmlSourceTest(ITestOutputHelper output) {
        _output = output;
        Env.Configure();
        Error.ExtraLogger = s => _output.WriteLine(s);
    }

    [Fact]
    public void Read() {
        PlantUmlSource source = new();
        source.Initialize(new Parameters(source, ["file=../../../schema/source/plantuml/sample.puml"]));
        Schema schema = Schema.CreateSchema(source);

        string schemaString = JsonUtils.JsonPretty(schema);

        _output.WriteLine(schemaString);

        string expected = File.ReadAllText("../../../schema/source/plantuml/sample.expected.json");
        Assert.Equal(expected.Trim(), schemaString.Trim());
    }
}
