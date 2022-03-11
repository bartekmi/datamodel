// TODO
// SchemaSource already has two ways of tweaking...
// 1. FilterModels (Used to show only latest API version models)
// 2. PostProcessSchema (Used to assign L2 based on toc.yaml in K8sToc.cs)
// ...Move both current uses in K8sSwaggerSource into that class


namespace datamodel.schema.tweaks {
    // We allow "tweaks" on top of the raw schema information passed in SchemaSource
    // One example of such a tweak is the introduction of inheritance - for example, Swagger
    // files do not have the concept of inheritance, even though the data structure obviously
    // implies it.
    public abstract class Tweak {
        public bool PostHydration { get; protected set; } 
        public abstract void Apply(TempSource source);
    }
}