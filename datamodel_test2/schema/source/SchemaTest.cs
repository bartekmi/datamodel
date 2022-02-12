using System;
using Xunit;

using Newtonsoft.Json;

namespace datamodel.schema.source {
    public class SchemaTest {
        [Fact]
        public void Hydrate() {
            SimpleSource source = new SimpleSource("../../../schema/simple_schema.json");
            Schema.CreateSchema(source);
            Schema schema = Schema.Singleton;

            Assert.Equal(3, schema.Models.Count);

            // Hydration = Inheritance
            Model dir = schema.FindByClassName("Directory");
            Assert.Equal("FileSystemObject", dir.Superclass.DbName);
        }
    }
}