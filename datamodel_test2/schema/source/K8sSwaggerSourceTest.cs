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
            var c = new K8sSwaggerSource.VersionComparer();

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
    }
}