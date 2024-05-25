using System;
using Xunit;

using datamodel.schema.source;

namespace datamodel.schema {
    public class SchemaTest {
        [Fact]
        public void Hydrate() {
            SimpleSource source = new();
            source.Initialize(new Parameters(source, new string[] { 
                "file=../../../schema/simple_schema.json",
                }));

            Schema schema = Schema.CreateSchema(source);

            // Hydration = Inheritance
            Model dir = schema.FindByQualifiedName("Directory");
            
            Assert.NotNull(dir);
            Assert.NotNull(dir.Superclass);
            Assert.NotNull(dir.Superclass.QualifiedName);

            Assert.Equal("FileSystemObject", dir.Superclass.QualifiedName);
        }
    }
}