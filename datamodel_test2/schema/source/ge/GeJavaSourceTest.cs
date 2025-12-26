
using System.Collections.Generic;
using System.IO;
using datamodel.utils;
using Xunit;
using Xunit.Abstractions;

namespace datamodel.schema.source.ge;

public class GeJavaSourceTest {
    private readonly ITestOutputHelper _output;

    public GeJavaSourceTest(ITestOutputHelper output) {
        _output = output;
        Env.Configure();
        Error.ExtraLogger = s => _output.WriteLine(s);
    }

    [Fact]
    public void ParseFieldJavadoc() {
        string javadoc = @"
    sourcing:
      - type: JSON
      source: TransferRequests
      description: 
      expression: $.StatusTimestamp
      example: stuff

      Extra line 1
      Extra line 2";

        Dictionary<string, string> actual = GeJavaSource.ParseFieldJavadoc(javadoc, out string trailintText);
        Assert.Equal(new Dictionary<string, string>() {
            ["type"] = "JSON",
            ["source"] = "TransferRequests",
            ["expression"] = "$.StatusTimestamp",
            ["example"] = "stuff"
        }, actual);

        Assert.Equal("Extra line 1\nExtra line 2", trailintText);
    }
}
