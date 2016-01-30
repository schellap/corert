using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace Packaging
{
    public class Packer
    {
        private static Dictionary<string, string> RuntimeIds = new Dictionary<string, string>()
        {
            { "Windows_NT".ToLower(), "win7-x64" },
            { "Linux".ToLower(), "ubuntu.14.04-x64" },
            { "OSX".ToLower(), "osx.10.10-x64" }
        };
 
        private string _version;
        private string Version
        {
            get
            {
                if (_version == null)
                    _version = Utils.Version(Platform, Milestone);
                return _version;
            }
        }

        private string _uid = "";
        private string Uid
        {
            get
            {
                if (_uid.Length == 0 && Milestone.Equals("testing"))
                    _uid = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8);
                return _uid;
            }
        }

        private string IlcStr = "Microsoft.DotNet.ILCompiler";
        private string IlcPkgStr
        {
            get
            {
                return string.IsNullOrEmpty(Uid) ? IlcStr : $"{IlcStr}.{Uid}";
            }
        }
        private string IlcSdkPkgStr
        {
            get
            {
                return $"{IlcStr}.SDK.{Uid}";
            }
        }
   
        private string LibPrefix
        {
            get
            {
                return string.Equals(Platform, "Windows_NT", StringComparison.OrdinalIgnoreCase) ? "" : "lib";
            }
        }

        private string StaticLibExt
        {
            get
            {
                return string.Equals(Platform, "Windows_NT", StringComparison.OrdinalIgnoreCase) ? "lib" : "a";
            }
        }

        private string RuntimeId
        {
            get
            {
                return RuntimeIds[Platform.ToLower()];
            }
        }

        private static Dictionary<string, string> LibExts = new Dictionary<string, string>()
        {
            { "Windows_NT".ToLower(), "dll" },
            { "Linux".ToLower(), "so" },
            { "OSX".ToLower(), "dylib" },
        };

        private string ExeExt
        {
            get
            {
                return Platform.ToLower().Equals("Windows_NT".ToLower()) ? ".exe" : "";
            }
        }

        private string LibExt
        {
            get
            {
                return LibExts[Platform.ToLower()];
            }
        }

        private string RootDir;
        private string Platform;
        private string Milestone;
        private string Flavor;
        private string Arch;
        private string RelProdBin { get; set; }
        private string PackageDir { get; set; }
        private string PublishDir        { get { return Path.Combine(PackageDir, "publish1"); } }
        private string PublishProjectDir { get { return Path.Combine(PackageDir, "stage1"); } }
        private bool PushJsonPkg;
        private string NuGetPath;
        private string NuGetHost
        {
            get
            {
                return Platform.ToLower().Equals("Windows_NT".ToLower()) ? null : "mono";
            }
        }
        private string DotNetPath;
 
        public Packer(string[] args)
        {
            ParseArgs(args);
        }
   
        private void ParseArgs(string[] args)
        {
            ArgumentSyntax.Parse(args, syntax =>
            {
                syntax.DefineOption("m|milestone", ref Milestone, "Toolchain milestone: nightly, testing, prerelease");
                syntax.DefineOption("os", ref Platform, "Supported: Windows_NT, Linux, OSX");
                syntax.DefineOption("type", ref Flavor, "Debug/Release");
                syntax.DefineOption("arch", ref Arch, "x64/x86/arm/arm64");
                syntax.DefineOption("root", ref RootDir, "RootDir of the repo");
                syntax.DefineOption("json-only", ref PushJsonPkg, "Pack or push the json package if true or the rid package");
            });
    
            RelProdBin = Path.Combine("bin", "Product", $"{Platform}.{Arch}.{Flavor}");
            PackageDir = Path.Combine(RootDir, RelProdBin, ".nuget");
            NuGetPath = Path.Combine(RootDir, "packages", "NuGet.exe");
            DotNetPath = Path.Combine(Path.Combine(RootDir, "bin", "tools", "cli"), "bin", "dotnet" + ExeExt);
        }
        
        private IEnumerable<NuSpecFileTag> GetILCompilerFiles()
        {
            List<string> managed = new List<string> {
                "ilc.exe",
                "ILCompiler.Compiler.dll",
                "ILCompiler.DependencyAnalysisFramework.dll",
                "ILCompiler.TypeSystem.dll"
            };
        
            List<string> native = new List<string>
            {
                "jitinterface." + LibExt
            };
    
            var managedSpec = managed.Select(s => new NuSpecFileTag(
                $"{RelProdBin}/{s}",
                $"runtimes/any/lib/dotnet/{s}"));
            var nativeSpec = native.Select(s => new NuSpecFileTag(
                $"{RelProdBin}/{s}",
                $"runtimes/{RuntimeId}/native/{s}"));
    
            return managedSpec.Concat(nativeSpec);
        }
    
        private IEnumerable<NuSpecFileTag> GetILCompilerSdkFiles()
        {
            var libFiles = new List<string> {
                "Runtime",
                "PortableRuntime",
                "bootstrapper",
                "bootstrappercpp"
            };
            if (!string.Equals(Platform, "Windows_NT", StringComparison.OrdinalIgnoreCase))
            {
                libFiles.Add("System.Private.CoreLib.Native");
            }
    
            var headerFiles = new List<string> {
                "Native/Bootstrap/common.h"
            };
    
            var managedFiles = new List<string> {
                "System.Private.CoreLib",
                "System.Private.DeveloperExperience.Console",
                "System.Private.Interop",
                "System.Private.Reflection",
                "System.Private.Reflection.Core",
                "System.Private.Reflection.Execution",
                "System.Private.Reflection.Metadata",
                "System.Private.StackTraceGenerator",
                "System.Private.Threading"
            };
    
            var libSpec = libFiles.Select(s => new NuSpecFileTag(
                $"{RelProdBin}/lib/{LibPrefix}{s}.{StaticLibExt}",
                $"runtimes/{RuntimeId}/native/sdk/{LibPrefix}{s}.{StaticLibExt}"));
            var headerSpec = headerFiles.Select(s => new NuSpecFileTag(
                $"src/{s}", $"runtimes/{RuntimeId}/native/inc/{Path.GetFileName(s)}"));
            var managedSpec = managedFiles.Select(s => new NuSpecFileTag(
                $"{RelProdBin}/{s}.dll",
                $"runtimes/{RuntimeId}/native/sdk/{s}.dll"));
            return libSpec.Concat(headerSpec).Concat(managedSpec);
        }
    
        public string Pack()
        {
            if (!File.Exists(NuGetPath))
            {
                Utils.DownloadFile("https://api.nuget.org/downloads/nuget.exe", NuGetPath);
            }
            Directory.Delete(PackageDir, true);
            Directory.CreateDirectory(PackageDir);
    
            NuSpecFile ilCompiler = new NuSpecFile
            {
                Id = $"toolchain.{RuntimeId}.{IlcPkgStr}",
                Version = Version,
                Title = "Microsoft .NET Native Toolchain",
                Description = "Provides the toolchain to compile managed code to native.",
                Files = GetILCompilerFiles(),
                Dependencies = Constants.Dependencies
            };
            NuSpecFile ilCompilerSdk = new NuSpecFile
            {
                Id = $"toolchain.{RuntimeId}.{IlcSdkPkgStr}",
                Version = Version,
                Title = "Microsoft .NET Native Toolchain SDK",
                Description = "Provides the toolchain SDK to compile managed code to native.",
                Files = GetILCompilerSdkFiles(),
                Dependencies = new Dictionary<string, string>() {
                    { $"toolchain.{RuntimeId}.{IlcPkgStr}", Version }
                }
            };
    
            if (!PushJsonPkg)
            {
                ilCompiler.Pack(PackageDir, NuGetPath, RootDir, NuGetHost);
                ilCompilerSdk.Pack(PackageDir, NuGetPath, RootDir, NuGetHost);
            }
    
            // runtime.json packages
            string[] names = { IlcPkgStr, IlcSdkPkgStr };
            NuSpecFile[] files = { ilCompiler, ilCompilerSdk };
    
            for (int i = 0; i < names.Length; ++i)
            {
                string runtimeJson = $"{names[i]}.runtime.json";
                File.WriteAllText(
                    Path.Combine(PackageDir, runtimeJson),
                    GetRuntimeJson(names[i], Version));
    
                files[i].Id = names[i];
                files[i].Files = new List<NuSpecFileTag> { new NuSpecFileTag(runtimeJson) };
                files[i].Pack(PackageDir, NuGetPath, PackageDir, NuGetHost);
            }

            Console.WriteLine("Pack completed... " + Version);
            return Version;
        }

        private void Publish()
        {
            if (Directory.Exists(PublishProjectDir))
                Directory.Delete(PublishProjectDir, true);
            Directory.CreateDirectory(PublishProjectDir);

            string output;
            string error;
            File.WriteAllText(
                Path.Combine(PublishProjectDir, "Program.cs"),
                "class H { static void Main() { } }");
            File.WriteAllText(
                Path.Combine(PublishProjectDir, "project.json"),
                GetProjectJson());

            string[] sources = new string[] {
                "https://www.myget.org/F/dotnet-core/",
                "https://www.myget.org/F/dotnet-coreclr/",
                "https://www.myget.org/F/dotnet-corefxtestdata/",
                "https://www.myget.org/F/dotnet/",
                "https://www.nuget.org/api/v2/"
            };
            string srcArg = string.Join(" ", sources.Select(s => $"-s \"{s}\""));
            Utils.Execute(DotNetPath, $"restore --quiet -s \"{PackageDir}\" {srcArg} \"{PublishProjectDir}\" --runtime \"{RuntimeId}\"", out output, out error);
            Utils.Execute(DotNetPath, $"publish \"{PublishProjectDir}\" --native-subdirectory -o \"{PublishDir}\" -f \"dnxcore50\" --runtime \"{RuntimeId}\"", out output, out error);
        }

        private bool EnsureRidPackages(string feedUrl)
        {
            string output;
            string error;
            if (Utils.Execute(NuGetPath, $"list -Source {feedUrl} {IlcStr} -PreRelease", out output, out error) != 0)
            {
                return false;
            }
            foreach (var pkg in new string[] { IlcPkgStr, IlcSdkPkgStr })
            {
                foreach (var rid in RuntimeIds.Values)
                {
                    if (!output.Contains($"toolchain.{rid}.{pkg}.{Version}"))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void Push()
        {
            if (Milestone.Equals("testing"))
            {
                Console.WriteLine("Won't push test packages as the package names have UIDs");
                return;
            }
            string feedUrl = Environment.GetEnvironmentVariable("CoreRT_FeedUrl");
            string feedAuth = Environment.GetEnvironmentVariable("CoreRT_FeedAuth");
            string[] names = new string[] {
                $"{IlcPkgStr}.{Version}.nupkg", 
                $"{IlcSdkPkgStr}.{Version}.nupkg"
            };
            if (PushJsonPkg && !EnsureRidPackages(feedUrl))
            {
                Console.WriteLine("Could not push json package as the rid packages were not all found");
                return;
            }
            string output, error;
            foreach (var name in names)
            {
                var pkg = (PushJsonPkg) ? name : $"toolchain.{RuntimeId}.{name}";
                var pushPkg = Path.Combine(PackageDir, pkg);
                Utils.Execute(NuGetPath, $"push \"{pushPkg}\" {feedAuth} -Source \"{feedUrl}\"", out output, out error);
            }
        }
    
        private string GetProjectJson()
        {
            return $@"{{
    ""version"": ""1.0.0-*"",
    ""compilationOptions"": {{
        ""emitEntryPoint"": true,
    }},
    ""dependencies"": {{
        ""NETStandard.Library"": ""1.0.0-rc2-23704"",
        ""Microsoft.NETCore.TestHost"": ""1.0.0-beta-23504"",
        ""toolchain.{RuntimeId}.{IlcSdkPkgStr}"": ""{Version}"",
    }},
    ""frameworks"": {{
        ""dnxcore50"": {{
            ""imports"": ""portable-net451+win8""
        }}
    }}
}}";
        }

        private string GetRuntimeJson(string name, string version, string prefix = "toolchain")
        {
            List<string> parts = new List<string>();
            foreach (var rid in RuntimeIds.Values)
            {
                parts.Add($@"""{rid}"": {{
                    ""{name}"": {{
                        ""{prefix}.{rid}.{name}"": ""{version}""
                    }}
                }}");
            }
    
            return $@"{{ ""runtimes"":
        {{
            {string.Join(",", parts)}
        }}
    }}";
        }
    
        public static int Main(string[] args)
        {
            switch (args[0])
            {
                case "pack":
                {
                    Packer packer = new Packer(args.Skip(1).ToArray());
                    packer.Pack();
                }
                break;

                case "publish":
                {
                    Packer packer = new Packer(args.Skip(1).ToArray());
                    packer.Publish();
                }
                break;

                case "push":
                {
                    Packer packer = new Packer(args.Skip(1).ToArray());
                    packer.Push();
                }
                break;

                default:
                {
                    Packer pack = new Packer(args);
                    pack.Pack();

                    pack.Publish();
                }
                break;
            }
            return 0;
        }
    }
}
