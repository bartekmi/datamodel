using System;
using Xunit;

using Newtonsoft.Json;

namespace datamodel.schema.source {
    public class SimpleSourceTest {
        [Fact]
        public void Read() {
            SimpleSource source = new SimpleSource("../../../schema/simple_schema.json");
            Schema.CreateSchema(source);
            Schema schema = Schema.Singleton;

            Assert.Equal(3, schema.Models.Count);
        }
    }
}