namespace datamodel.schema {
    public interface IDbElement {
        string Name { get; set; }
        string Team { get; set; }
        string Description { get; set; }
        bool Deprecated { get; set; }
    }
}