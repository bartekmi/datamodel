using System;
using System.Text.RegularExpressions;
using System.Linq;

using Xunit;

using Newtonsoft.Json;

using datamodel.schema.tweaks;

namespace datamodel.schema.source.from_data {
    public class JsonSourceClusterTest {

        [Fact]
        public void ClusterTest() {
            Env.Configure();

            JsonSource source = new JsonSource();
            source.Initialize(new Parameters(source, new string[] { 
                @"files=
                    ../../../schema/source/from_data/json_source_cluster_1.json,
                    ../../../schema/source/from_data/json_source_cluster_2.json,
                    ../../../schema/source/from_data/json_source_cluster_3.json,"
                }));


            string json = FromDataUtils.ToJasonNoQuotes(source);

            // Console.WriteLine(json);

            Assert.Equal(@"{
  Models: {
    cluster1: {
      Labels: [
        {
          Name: Instance Count,
          Value: 2
        }
      ],
      Name: cluster1,
      QualifiedName: cluster1,
      Levels: [
        cluster1
      ],
      AllProperties: [
        {
          Name: a,
          DataType: Integer
        },
        {
          Name: b,
          CanBeEmpty: true,
          DataType: Integer
        },
        {
          Name: c,
          CanBeEmpty: true,
          DataType: Integer
        }
      ]
    },
    cluster1.obj1: {
      Labels: [
        {
          Name: Instance Count,
          Value: 2
        }
      ],
      Name: obj1,
      QualifiedName: cluster1.obj1,
      Levels: [
        cluster1
      ],
      AllProperties: [
        {
          Name: n,
          DataType: String
        }
      ]
    },
    cluster2: {
      Labels: [
        {
          Name: Instance Count,
          Value: 1
        }
      ],
      Name: cluster2,
      QualifiedName: cluster2,
      Levels: [
        cluster2
      ],
      AllProperties: [
        {
          Name: z,
          DataType: Integer
        }
      ]
    },
    cluster2.obj999: {
      Labels: [
        {
          Name: Instance Count,
          Value: 1
        }
      ],
      Name: obj999,
      QualifiedName: cluster2.obj999,
      Levels: [
        cluster2
      ],
      AllProperties: [
        {
          Name: n,
          DataType: String
        }
      ]
    }
  },
  Associations: [
    {
      OwnerSide: cluster1,
      OwnerMultiplicity: Aggregation,
      OtherSide: cluster1.obj1,
      OtherMultiplicity: ZeroOrOne
    },
    {
      OwnerSide: cluster2,
      OwnerMultiplicity: Aggregation,
      OtherSide: cluster2.obj999,
      OtherMultiplicity: ZeroOrOne
    }
  ]
}", json);
        }
    }
}