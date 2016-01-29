using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Net.Http;

public class Packaging
{
    private string[] args;

    public Packaging(string[] args)
    {
        this.args = args;
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
        });

        ProductBin = Path.Combine(RootDir, "bin", "Product", $"{Platform}.{Arch}.{Flavor}");
        PackageDir = Path.Combine(ProductBin, ".nuget");
        NuGetPath = Path.Combine(RootDir, "packages", "NuGet.exe");
    }

    private string LibPrefix
    {
        get
        {
            return string.Equals(Platform, "Windows_NT", StringComparison.OrdinalIgnoreCase) ? "lib" : "";
        }
    }
    private string StaticLibExt
    {
        get
        {
            return string.Equals(Platform, "Windows_NT", StringComparison.OrdinalIgnoreCase) ? "lib" : "a";
        }
    }

    Dictionary<string, string> RuntimeIds = new Dictionary<string, string>()
    {
        { "Windows_NT".ToLower(), "win7-x64" },
        { "Linux".ToLower(), "ubuntu.14.04-x64" },
        { "OSX".ToLower(), "osx.10.10-x64" }
    };

    public string RootDir;
    public string Platform;
    public string Milestone;
    public string Flavor;
    public string Arch;
    public string ProductBin { get; private set; }
    public string PackageDir { get; private set; }
    public string RuntimeId
    {
        get
        {
            return RuntimeIds[Platform.ToLower()];
        }
    }
    public string NuGetPath;

    private static int Execute(string command, string arguments, out string output, out string error)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        var process = new Process
        {
            StartInfo = psi
        };
        process.EnableRaisingEvents = true;
        process.Start();
        output = process.StandardOutput.ReadToEnd();
        error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (output.Length > 0)
        {
            Console.WriteLine(output);
        }
        if (error.Length > 0)
        {
            Console.WriteLine(error);
        }
        return process.ExitCode;
    }

    struct NuSpecFile
    {
        public string Id;
        public string Version;
        public string Title;
        public string Description;
        public IEnumerable<NuSpecFileTag> Files;
        public IEnumerable<KeyValuePair<string, string>> Dependencies;

        public string FilesString
        {
            get
            {
                return string.Join(Environment.NewLine, Files.Select(f => f.ToString()));
            }
        }

        public string DependenciesString
        {
            get
            {
                return string.Join(Environment.NewLine, Dependencies.Select(s => $"<dependency id=\"{s.Key}\" version=\"{s.Value}\" />"));
            }
        }

        public override string ToString()
        {
            return $@"<?xml version=""1.0""?>
<package>
  <metadata>
    <id>{Id}</id>
    
    <version>{Version}</version>
    <title>{Title}</title>
    <authors>Microsoft</authors>
    <owners>Microsoft</owners>
    <licenseUrl>http://go.microsoft.com/fwlink/?LinkId=329770</licenseUrl>
    <projectUrl>https://github.com/dotnet/corert</projectUrl>
    <iconUrl>http://go.microsoft.com/fwlink/?LinkID=288859</iconUrl>
    <requireLicenseAcceptance>true</requireLicenseAcceptance>
    <description>{Description}</description>
    <releaseNotes>Initial release</releaseNotes>
    <copyright>Copyright &#169; Microsoft Corporation</copyright>
    <dependencies>
    {DependenciesString}
    </dependencies>
  </metadata>
  <files>
  {FilesString}
  </files>
</package>";
        }

        public void Write(string filePath)
        {
            File.WriteAllText(filePath, ToString());
        }

        public int Pack(string packageDir, string nugetPath, string basePath)
        {
            string nuspecFile = Path.Combine(packageDir, $"{Id}.nuspec");
            Write(nuspecFile);
            string output;
            string error;
            return Execute(
                nugetPath,
                $"pack \"{nuspecFile}\" -NoPackageAnalysis -NoDefaultExcludes -BasePath \"{basePath}\" -OutputDirectory \"{packageDir}\"",
                out output, out error);
        }
    }

    struct NuSpecFileTag
    {
        public string Source { get; internal set; }
        public string Target { get; internal set; }
        public NuSpecFileTag(string src, string target = "")
        {
            Source = src;
            Target = target;
        }
        public override string ToString()
        {
            return string.IsNullOrEmpty(Target)
                ? $"<file src=\"{Source}\" />"
                : $"<file src=\"{Source}\" target=\"{Target}\" />";
        }
    }

    private IEnumerable<NuSpecFileTag> GetILCompilerFiles()
    {
        List<string> managed = new List<string> {
            "ilc.exe",
            "ILCompiler.Compiler.dll",
            "ILCompiler.DependencyAnalysisFramework.dll",
            "ILCompiler.TypeSystem.dll"
        };


        Dictionary<string, string> extension = new Dictionary<string, string>()
        {
            { "Windows_NT".ToLower(), "dll" },
            { "Linux".ToLower(), "so" },
            { "OSX".ToLower(), "dylib" },
        };

        List<string> native = new List<string>
        {
            "jitinterface." + extension[Platform.ToLower()]
        };

        var managedSpec = managed.Select(s => new NuSpecFileTag(s, $"runtimes/any/lib/dotnet/{s}"));
        var nativeSpec = native.Select(s => new NuSpecFileTag(
            $"{ProductBin}/{s}",
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
        if (string.Equals(Platform, "Windows_NT", StringComparison.OrdinalIgnoreCase))
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
            $"{ProductBin}/lib/{LibPrefix}{s}.{StaticLibExt}"));
        var headerSpec = headerFiles.Select(s => new NuSpecFileTag(
            $"src/{s}", $"runtimes/{RuntimeId}/native/inc/{Path.GetFileName(s)}"));
        var managedSpec = managedFiles.Select(s => new NuSpecFileTag(
            $"{ProductBin}/{s}.dll",
            $"runtimes/{RuntimeId}/native/sdk/{s}.dll"));
        return libSpec.Concat(headerSpec).Concat(managedSpec);
    }

    private IEnumerable<KeyValuePair<string, string>> GetDependencies()
    {
        return new Dictionary<string, string>() {
            { "Microsoft.DiaSymReader", "1.0.6" },
            { "Microsoft.DotNet.ObjectWriter", "1.0.4-prerelease-00001" },
            { "Microsoft.DotNet.RyuJit", "1.0.3-prerelease-00001" },
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

    public void Package()
    {
        if (!File.Exists(NuGetPath))
        {
            DownloadFile("https://api.nuget.org/downloads/nuget.exe", NuGetPath);
        }
        Directory.Delete(PackageDir, true);
        Directory.CreateDirectory(PackageDir);
        string packageVersion = Version();
        string ilcPkgStr = "Microsoft.DotNet.ILCompiler";
        string ilcPkgStrSdk = "Microsoft.DotNet.ILCompiler.SDK";
        if (Milestone.Equals("testing"))
        {
            var guid = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 8);
            ilcPkgStr = ilcPkgStr + "." + guid;
            ilcPkgStrSdk = ilcPkgStrSdk + "." + guid;
        }
        NuSpecFile ilCompiler = new NuSpecFile
        {
            Id = $"toolchain.{RuntimeId}.{ilcPkgStr}",
            Version = packageVersion,
            Title = "Microsoft .NET Native Toolchain",
            Description = "Provides the toolchain to compile managed code to native.",
            Files = GetILCompilerFiles(),
            Dependencies = GetDependencies()
        };
        NuSpecFile ilCompilerSdk = new NuSpecFile
        {
            Id = $"toolchain.{RuntimeId}.{ilcPkgStrSdk}",
            Version = packageVersion,
            Title = "Microsoft .NET Native Toolchain",
            Description = "Provides the toolchain to compile managed code to native.",
            Files = GetILCompilerFiles(),
            Dependencies = new Dictionary<string, string>() {
                { "Microsoft.DotNet.ILCompiler", packageVersion }
            }
        };

        ilCompiler.Pack(PackageDir, NuGetPath, RootDir);
        ilCompilerSdk.Pack(PackageDir, NuGetPath, RootDir);

        // runtime.json packages
        string[] names = { ilcPkgStr, ilcPkgStrSdk };
        NuSpecFile[] files = { ilCompiler, ilCompilerSdk };

        for (int i = 0; i < names.Length; ++i)
        {
            string runtimeJson = Path.Combine(PackageDir, $"{names[i]}.runtime.json");
            File.WriteAllText(runtimeJson, GetRuntimeJson(names[i], packageVersion));

            var file = files[i];
            file.Id = names[i];
            file.Files = new List<NuSpecFileTag> { new NuSpecFileTag(runtimeJson) };
            file.Pack(PackageDir, NuGetPath, RootDir);
        }
    }

    private async void DownloadFile(string requestUri, string filePath)
    {
        using (var httpClient = new HttpClient())
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                using (
                    Stream contentStream = await (await httpClient.SendAsync(request)).Content.ReadAsStreamAsync(),
                    stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 1024*1024, true))
                {
                    await contentStream.CopyToAsync(stream);
                }
            }
        }
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
        Packaging p = new Packaging(args);
        p.Package();
        return 0;
    }

    private string Version()
    {
        string output;
        string error;
        string version = "1.0.4";
        Execute("git", "rev-list --count HEAD", out output, out error);
        string timeZoneId = (string.Equals(Platform, "Windows_NT", StringComparison.OrdinalIgnoreCase))
                          ? "Pacific Standard Time" : "America/Los_Angeles";
        TimeZoneInfo tzPST = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        string dateSuffix = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tzPST).ToString("MMdd");
        return version + "-" + Milestone + "-" + dateSuffix + "-" + output.Trim().PadLeft(6, '0');
    }
}


