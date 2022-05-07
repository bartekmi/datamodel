using System;
using Xunit;

using Newtonsoft.Json;

namespace datamodel.schema.source {
    public class SimpleSourceTest {
        [Fact]
        public void Read() {
            SimpleSource source = new SimpleSource();
            source.Initialize(new Parameters(source, new string[] { "file=../../../schema/simple_schema.json" }));
            Schema.CreateSchema(source);
            Schema schema = Schema.Singleton;

            Assert.Equal(12, schema.Models.Count);
        }
    }
}