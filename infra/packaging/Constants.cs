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
    
            { "Microsoft.DiaSymReader", "1.0.6" },
            { "System.AppContext", "4.0.0" },
            { "System.Collections", "4.0.10" },
            { "System.Collections.Concurrent", "4.0.10" },
            { "System.Collections.Immutable", "1.1.37" },
            { "System.Console", "4.0.0-rc2-23616" },
            { "System.Diagnostics.Debug", "4.0.10" },
            { "System.Diagnostics.Tracing", "4.0.20" },
            { "System.IO", "4.0.10" },
            { "System.IO.FileSystem", "4.0.0" },
            { "System.IO.MemoryMappedFiles", "4.0.0-rc2-23616" },
            { "System.Linq", "4.0.0" },
            { "System.Reflection", "4.0.10" },
            { "System.Reflection.Extensions", "4.0.0" },
            { "System.Reflection.Metadata", "1.1.0" },
            { "System.Reflection.Primitives", "4.0.0" },
            { "System.Resources.ResourceManager", "4.0.0" },
            { "System.Runtime", "4.0.20" },
            { "System.Runtime.Extensions", "4.0.10" },
            { "System.Runtime.InteropServices", "4.0.20" },
            { "System.Text.Encoding", "4.0.10" },
            { "System.Text.Encoding.Extensions", "4.0.10" },
            { "System.Threading", "4.0.10" },
            { "System.Threading.Tasks", "4.0.10" },
            { "System.Xml.ReaderWriter", "4.0.0" },
            { "System.Runtime.InteropServices.RuntimeInformation", "4.0.0-beta-23504" }
        };
    }
}
