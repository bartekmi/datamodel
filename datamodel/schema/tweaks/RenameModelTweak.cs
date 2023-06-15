using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace datamodel.schema.tweaks {
    public class RenameModelTweak : Tweak {
        // Remove a particular suffix from model names
        public string SuffixToRemove;

        public RenameModelTweak() : base(TweakApplyStep.PreHydrate) {}

        public override void Apply(TempSource source) {
            foreach (var item in source.Models.ToList())
                if (item.Key.EndsWith(SuffixToRemove))
                    source.RenameModel(item.Value, RemoveSuffix(item.Key), RemoveSuffix(item.Value.Name));
        }

        private string RemoveSuffix(string text) {
            if (text.EndsWith(SuffixToRemove))
                return text.Substring(0, text.Length - SuffixToRemove.Length);
            return text;
        }
   }
}