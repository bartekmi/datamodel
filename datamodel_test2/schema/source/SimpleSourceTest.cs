using Xunit;

namespace datamodel.schema.source {
    public class SimpleSourceTest {
        [Fact]
        public void Read() {
            SimpleSource source = new();
            source.Initialize(new Parameters(source, new string[] { "file=../../../schema/simple_schema.json" }));
            Schema schema = Schema.CreateSchema(source);

            Assert.Equal(12, schema.Models.Count);
        }
    }
}