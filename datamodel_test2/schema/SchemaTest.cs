using System;
using Xunit;

using datamodel.schema.source;

namespace datamodel.schema {
    public class SchemaTest {
        [Fact]
        public void Hydrate() {
            SimpleSource source = new SimpleSource("../../../schema/simple_schema.json");
            Schema.CreateSchema(source);
            Schema schema = Schema.Singleton;

            // Hydration = Inheritance
            Model dir = schema.FindByClassName("Directory");
            Assert.Equal("FileSystemObject", dir.Superclass.FullyQualifiedName);
        }
    }
}