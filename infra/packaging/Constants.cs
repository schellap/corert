using System;
using System.Collections;
using System.Collections.Generic;

namespace Packaging
{
    class Constants
    {
        public static string IlcVersion = "1.0.4";
        public static IEnumerable<KeyValuePair<string, string>> Dependencies = new Dictionary<string, string>() {
            { "Microsoft.DotNet.ObjectWriter", "1.0.4-prerelease-00001" },
            { "Microsoft.DotNet.RyuJit", "1.0.3-prerelease-00001" },
            { "NETStandard.Library", "1.0.0-rc2-23704" },
            { "System.Collections.Immutable", "1.1.37" },
            { "System.IO.MemoryMappedFiles", "4.0.0-rc2-23616" },
            { "System.Reflection.Metadata", "1.1.0" },
            { "System.Xml.ReaderWriter", "4.0.0" },
            { "Microsoft.DiaSymReader", "1.0.6" }
        };
    }
}
