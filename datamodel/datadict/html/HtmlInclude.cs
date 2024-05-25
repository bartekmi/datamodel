using System.IO;

namespace datamodel.datadict.html {
    public class HtmlInclude : HtmlBase {

        private string _includeFilePath;

        public HtmlInclude(string includeFilePath) {
            _includeFilePath = includeFilePath;
        }

        public override void ToHtml(TextWriter writer, int indent) {
          if (!File.Exists(_includeFilePath)) {
            Error.Log("Expected file does not exist " + _includeFilePath);
            return;
          }

          writer.WriteLine(File.ReadAllText(_includeFilePath));
        }
    }
}