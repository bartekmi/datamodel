using System;
using System.IO;
using System.Collections.Generic;

using Xunit;
using Xunit.Abstractions;

namespace datamodel.schema.source {
    public class K8sSwaggerSourceTest {

        private readonly ITestOutputHelper _output;

        public K8sSwaggerSourceTest(ITestOutputHelper output) {
            _output = output;
        }

        [Fact]
        public void VersionComparer() {
            var c = new FilterOldApiVersionsTweak.VersionComparer();

            Assert.True(c.Compare("v1", "v1") == 0);
            Assert.True(c.Compare("v1beta1", "v1beta1") == 0);

            // vx-beta is less than vx
            Assert.True(c.Compare("v1", "v1beta1") > 0);
            Assert.True(c.Compare("v2beta1", "v2") < 0);

            // Version has precedence, beta status ignored
            Assert.True(c.Compare("v1", "v2") < 0);
            Assert.True(c.Compare("v1beta2", "v2beta1") < 0);
            Assert.True(c.Compare("v1beta1", "v2") < 0);
            Assert.True(c.Compare("v1", "v2beta1") < 0);

            // If version same and both have beta, compare the betas
            Assert.True(c.Compare("v1beta2", "v1beta1") > 0);
            Assert.True(c.Compare("v1beta1", "v1beta2") < 0);
        }

        [Fact]
        public void PopulateEnumDefinitions() {
            Env.Configure();
            
            Enum theEnum = new Enum();
            theEnum.Add("OnDelete", "");
            theEnum.Add("RollingUpdate", "");

            Column column = new Column() {
                Enum = theEnum,
                Description = "Type of daemon set update. Can be \"RollingUpdate\" or \"OnDelete\". Default is RollingUpdate.\n\nPossible enum values:\n - `\"OnDelete\"` Replace the old daemons only when it's killed\n - `\"RollingUpdate\"` Replace the old daemons by new ones using rolling update i.e replace them on each node one after the other.",
            };

            K8sSwaggerSource.PopulateEnumDefinitions("model", column);

            Assert.Equal("Replace the old daemons only when it's killed", theEnum.GetDescription("OnDelete"));
            Assert.Equal("Replace the old daemons by new ones using rolling update i.e replace them on each node one after the other.", theEnum.GetDescription("RollingUpdate"));
        }
    }
}