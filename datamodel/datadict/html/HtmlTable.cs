using System;
using System.Collections.Generic;
using System.IO;

namespace datamodel.datadict.html {
    public class HtmlTable : HtmlElement {

        public HtmlTable() : base("table") { }

        public HtmlTable(HtmlTr headerRow, IEnumerable<HtmlTr> dataRows) : this() {
            Add(headerRow);
            foreach (HtmlTr dataRow in dataRows)
                Add(dataRow);
        }

        public void AddTr(HtmlTr tr) {
            Add(tr);
        }
    }
}