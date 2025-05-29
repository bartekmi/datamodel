using System;
using System.Collections.Generic;
using System.Linq;

namespace datamodel.schema.source.botocore;

public class BotoService {
  public BotoMetadata Metadata;
  public Dictionary<string, BotoOperation> Operations = [];
  public Dictionary<string, BotoShape> Shapes = [];
  public string Documentation;
}

public class BotoMetadata {
  public string ServiceFullName;
  public string ServiceId;
  public string EndpointPrefix;
}

public class BotoOperation {
  public string Name;
  public BotoHttp Http;
  public BotoShapeReference Input;
  public BotoShapeReference Output;
  public List<BotoShapeReference> Errors;
  public string Documentation;
  public bool Idempotent;

  // Derived
  public bool IsListOp => Name.StartsWith("List");
  public bool IsGetOp => Name.StartsWith("Get") || Name.StartsWith("Describe");

  public override string ToString() {
    return Name;
  }
}

public class BotoHttp {
  public string Method;
  public string RequestUri;
  public int ResponseCode;
}

public class BotoShapeReference {
  public string Shape;
  public string Documentation;
  public string Location; // E.g. "querystring"
  public string LocatinName; // Perhaps a placeholder in an URL query string?

  public override string ToString() {
    return Shape;
  }
}

public class BotoShape {
  private readonly string[] NON_PRIMITIVE_TYPES = ["list", "map", "structure"];

  // Composite type values: list, map, structure
  // Primitive type values: blob, boolean, double, integer, long, string, timestamp, blob, float
  public string Type;

  // Specific to Structure
  public Dictionary<string, BotoShapeReference> Members;
  public List<string> Required = [];   // List of key names in the Members dict.
  public bool Exception;

  // Specific for List
  public BotoShapeReference Member;

  // Specific for Map
  public BotoShapeReference Key;
  public BotoShapeReference Value;

  // Specific to primitive types
  public double Max;
  public double Min;
  public string Pattern;
  public bool Sensitive;
  public bool Box;    // If true, should be treated as primitive
  public List<string> Enum;

  // General
  public string Documentation;
  public Dictionary<string, string> Labels = [];  // Will be added to Model Labels

  // Not read from JSON but set for convenience
  public string ShapeName;
  public Model Model;

  // Derived
  public bool IsNonPrimitive => NON_PRIMITIVE_TYPES.Contains(Type);
  public bool IsPrimitive => !IsNonPrimitive;
  public bool IsList => Type == "list";
  public bool IsStructure => Type == "structure";
  public bool IsMap => Type == "map";

  public bool IsRequired(string memberName) { return Required.Contains(memberName); }

  public override string ToString() {
    return ShapeName;
  }

  internal bool IsListOfPrimitive(Dictionary<string, BotoShape> mappings) {
    if (!IsList)
      return false;
    BotoShape member = mappings[Member.Shape];
    return member.IsPrimitive;
  }
}
