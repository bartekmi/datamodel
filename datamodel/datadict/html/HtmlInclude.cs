using System.IO;

namespace datamodel.datadict.html {
    public class HtmlInclude : HtmlBase {

        private string _includeFilePath;

        public HtmlInclude(string includeFilePath) {
            _includeFilePath = includeFilePath;
        }

        public override void ToHtml(TextWriter writer, int indent) {
            writer.WriteLine(File.ReadAllText(_includeFilePath));
        }
    }
}