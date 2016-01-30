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

        public int Pack(string packageDir, string nugetPath, string basePath, string nugetHost)
        {
            string nuspecFile = Path.Combine(packageDir, $"{Id}.nuspec");
            Write(nuspecFile);
            string output;
            string error;
            string command = (string.IsNullOrEmpty(nugetHost)) ? nugetPath : nugetHost;
            string hosted = (string.IsNullOrEmpty(nugetHost)) ? "" : nugetPath;
            return Utils.Execute(command,
                $"{hosted} pack \"{nuspecFile}\" -NoPackageAnalysis -NoDefaultExcludes -BasePath \"{basePath}\" -OutputDirectory \"{packageDir}\"",
                out output, out error, packageDir);
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
}
