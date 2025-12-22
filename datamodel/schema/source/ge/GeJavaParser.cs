using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace datamodel.schema.source.ge;

// This class assumes you've extracted Java information using the ge_extract project
public static class GeJavaParser {
    public const string MODEL_FILES_EXTRACTION = "/tmp/datamodel/java_models.yaml";

    public static void AddInfoFromJava(GeYamlSource yamlSource) {
        Extraction extraction = LoadExtraction(MODEL_FILES_EXTRACTION);
        AddNewProperties(yamlSource, extraction);
    }

    public static Extraction LoadExtraction(string path) {
        IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        string yaml = File.ReadAllText(path);
        return deserializer.Deserialize<Extraction>(yaml);
    }

    private static void AddNewProperties(GeYamlSource yamlSource, Extraction extraction) {
        string suffix = "Model";
        foreach (ClassInfo result in extraction.classInfos) {
            if (result.className == "BaseModel")
                continue;

            // Possibly, strip of a suffix from the class name
            string modelName = result.className;
            if (result.className.EndsWith(suffix))
                modelName = result.className.Substring(0, result.className.Length - suffix.Length);
            else
                Console.WriteLine("WARNING: Model {0} does not end with suffix {1}.", result.className, suffix);

            // Find the Model
            Model model = yamlSource.GetModels().SingleOrDefault(x => x.QualifiedName == modelName);
            if (model == null) {
                Console.WriteLine("WARNING: Model {0} not found in YAML files. Skipping.", modelName);
                continue;
            }

            // Loop over the properties
            HashSet<string> propertyNames = new(model.AllProperties.Select(x => x.Name.ToLower()));
            foreach (FieldInfo field in result.privateFields) {
                if (propertyNames.Contains(field.name.ToLower()))
                    continue;   // Nothing new here

                Console.WriteLine("WARNING: Property {0}.{1} exists in code, but not YAML",
                    model.QualifiedName, field.name);
            }
        }
    }

}

#region Java Model Classes

public class Extraction {
    public string directory;
    public List<ClassInfo> classInfos;
}

public class ClassInfo {
    public string sourceFile;
    public string className;
    public string javaDoc;
    public List<FieldInfo> privateFields;
    public List<MethodInfo> publicStaticMethods;
}

public class FieldInfo {
    public string name;
    public string type;
    public string javaDoc;
    public bool isArray;
}

public class MethodInfo {
    public string name;
    public string returnType;
    public List<string> parameterTypes;
    public string javaDoc;
}

#endregion
