namespace datamodel.schema {
    public interface IDbElement {
        string DbName { get; set; }
        string Team { get; set; }
        string Description { get; set; }
        Visibility Visibility { get; set; }
        bool Deprecated { get; set; }
        string Issue { get; set; }
        string Group { get; set; }
    }
}