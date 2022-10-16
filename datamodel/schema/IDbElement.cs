using System;
using System.Collections.Generic;
namespace datamodel.schema {
    public interface IDbElement {
        string Name { get; set; }
        string Description { get; set; }
        bool Deprecated { get; set; }
        List<Label> Labels { get; set; }
    }

    public static class IDbElementExtensions {
        public static string[] DescriptionParagraphs(this IDbElement element) {
            if (string.IsNullOrWhiteSpace(element.Description))
                return new string[0];
            return element.Description.Split("\n", StringSplitOptions.RemoveEmptyEntries);
        }

    }
}