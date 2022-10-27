using System;
using System.IO;
using System.Text;

namespace datamodel.schema.source.protobuf {
    public static class ProtobufCommentProcessor {
        public static string ProcessComments(string raw) {
            string line = null;
            bool startNewParagraph = true;
            StringBuilder builder = new StringBuilder();

            using (TextReader reader = new StringReader(raw)) {
                while ((line = reader.ReadLine()) != null) {
                    if (string.IsNullOrWhiteSpace(line)) {
                        if (builder.Length == 0)        // No leading empty lines
                            continue;
                        builder.AppendLine();
                        startNewParagraph = true;
                    } else {
                        if (startNewParagraph)
                            startNewParagraph = false;
                        else
                            builder.Append(' ');
                        builder.Append(line.Trim());
                    }
                }
            }

            return builder.ToString().TrimEnd();
        }
    }
}