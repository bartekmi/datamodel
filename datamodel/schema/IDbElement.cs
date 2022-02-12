namespace datamodel.schema {
    public interface IDbElement {
        string Name { get; set; }
        string Level1 { get; set; }
        string Description { get; set; }
        bool Deprecated { get; set; }
    }
}