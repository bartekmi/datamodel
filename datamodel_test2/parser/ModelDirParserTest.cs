using System;
using System.IO;
using Xunit;

namespace datamodel.metadata {
    public class ModelDirParserTest {
        [Fact]
        public void IsActiveRecord() {
            string fileContents = @"
# TEAM: customs
# WATCHERS: ebeweber, newhouse

class Customs::HsCode < ApplicationRecord
  include AlgoliaSearch";
            using (StringReader reader = new StringReader(fileContents)) {
                bool isActiveRecord = ModelDirParser.IsActiveRecord(reader, out string className, out string team);

                Assert.True(isActiveRecord);
                Assert.Equal("Customs::HsCode", className);
                Assert.Equal("customs", team);
            }
        }
    }
}
