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
                @"paths=
                    ../../../schema/source/from_data/json_source_cluster_1.json,
                    ../../../schema/source/from_data/json_source_cluster_2.json,
                    ../../../schema/source/from_data/json_source_cluster_3.json,"
                }));


            string json = FromDataUtils.ToJasonNoQuotes(source);

            // Console.WriteLine(json);

            Assert.Equal(@"{
  Models: {
    cluster1: {
      Name: cluster1,
      QualifiedName: cluster1,
      Levels: [
        cluster1
      ],
      AllProperties: [
        {
          DataType: Integer,
          Name: a
        },
        {
          CanBeEmpty: true,
          DataType: Integer,
          Name: b
        },
        {
          CanBeEmpty: true,
          DataType: Integer,
          Name: c
        }
      ],
      Labels: [
        {
          Name: Instance Count,
          Value: 2
        }
      ]
    },
    cluster1.obj1: {
      Name: obj1,
      QualifiedName: cluster1.obj1,
      Levels: [
        cluster1
      ],
      AllProperties: [
        {
          DataType: String,
          Name: n
        }
      ],
      Labels: [
        {
          Name: Instance Count,
          Value: 2
        }
      ]
    },
    cluster2: {
      Name: cluster2,
      QualifiedName: cluster2,
      Levels: [
        cluster2
      ],
      AllProperties: [
        {
          DataType: Integer,
          Name: z
        }
      ],
      Labels: [
        {
          Name: Instance Count,
          Value: 1
        }
      ]
    },
    cluster2.obj999: {
      Name: obj999,
      QualifiedName: cluster2.obj999,
      Levels: [
        cluster2
      ],
      AllProperties: [
        {
          DataType: String,
          Name: n
        }
      ],
      Labels: [
        {
          Name: Instance Count,
          Value: 1
        }
      ]
    }
  },
  Associations: [
    {
      OwnerSide: cluster1,
      OwnerMultiplicity: Aggregation,
      OtherSide: cluster1.obj1,
      OtherMultiplicity: One
    },
    {
      OwnerSide: cluster2,
      OwnerMultiplicity: Aggregation,
      OtherSide: cluster2.obj999,
      OtherMultiplicity: One
    }
  ]
}", json);
        }
    }
}