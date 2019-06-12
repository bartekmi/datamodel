using System;
using System.Collections.Generic;
using System.Text;

namespace datamodel.utils {
    public static class EnumUtils {
        public static T Parse<T>(string value) {
            return (T)Enum.Parse(typeof(T), value, false);
        }

        public static T TryParse<T>(string input, T defaultValue) where T : struct {
            if (Enum.TryParse<T>(input, true, out T result))
                return result;
            return defaultValue;
        }
    }
}
