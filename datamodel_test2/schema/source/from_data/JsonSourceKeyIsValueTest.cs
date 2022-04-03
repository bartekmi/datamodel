using System;

using Xunit;
using Xunit.Abstractions;

using Newtonsoft.Json;

namespace datamodel.schema.source.from_data {
    public class JsonSourceKeyIsValueTest {

        [Fact]
        public void KeyIsValue() {
            Env.Configure();

            TextSource text = TextSource.Text(@"{
    key_is_value: {
        one: {
            z: 7
        },
        two: {
            z: 8
        }
    }
}");

            JsonSource source = new JsonSource(text,
                new JsonSource.Options() {
                    PathsWhereKeyIsData = new string[] {
                        ".key_is_value",
                    }
                }
            );

            string json = FromDataUtils.ToJasonNoQuotes(source, false);

            Assert.True(json.Replace(" ", "")
                .Contains(@"{
          Labels: [
            {
              Name: Example,
              Value: one
            }
          ],
          Name: __key__,
          DataType: string
        }".Replace(" ", "")), json);
        }
    }
}