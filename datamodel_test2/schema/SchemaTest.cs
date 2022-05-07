using System;
using Xunit;

using datamodel.schema.source;

namespace datamodel.schema {
    public class SchemaTest {
        [Fact]
        public void Hydrate() {
            SimpleSource source = new SimpleSource();
            source.Initialize(new Parameters(source, new string[] { 
                "file=../../../schema/simple_schema.json",
                }));

            Schema.CreateSchema(source);
            Schema schema = Schema.Singleton;

            // Hydration = Inheritance
            Model dir = schema.FindByQualifiedName("Directory");
            Assert.Equal("FileSystemObject", dir.Superclass.QualifiedName);
        }
    }
}