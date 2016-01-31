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
    class Utils
    {
        public static int Execute(string command, string arguments, out string output, out string error)
        {
            return Execute(command, arguments, out output, out error, null);
        }

        public static int Execute(string command, string arguments, out string output, out string error, string workingDir)
        {
            Console.WriteLine(command + " " + arguments);
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDir ?? Directory.GetCurrentDirectory()
            };
            var process = new Process
            {
                StartInfo = psi
            };
            process.EnableRaisingEvents = true;
            process.Start();
            // TODO: Make out/error async
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
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(command + " " + arguments + " exited with code: " + process.ExitCode);
            }
            return process.ExitCode;
        }

        public static async void DownloadFile(string uri, string path)
        {
            using (var client = new HttpClient())
            using (var req = new HttpRequestMessage(HttpMethod.Get, uri))
            using (Stream
                   content = await (await client.SendAsync(req)).Content.ReadAsStreamAsync(),
                   file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1024*1024, true))
            {
                await content.CopyToAsync(file);
            }
        }

        public static string Version(string platform, string milestone)
        {
            string output;
            string error;
            Execute("git", "rev-list --count HEAD", out output, out error);
            // string timeZoneId = (string.Equals(platform, "Windows_NT", StringComparison.OrdinalIgnoreCase))
            //                   ? "Pacific Standard Time" : "America/Los_Angeles";
            // TimeZoneInfo tzPST = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            // string dateSuffix = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tzPST).ToString("yyMMdd");
            string dateSuffix = "";
            return Constants.IlcVersion + "-" + milestone + "-" + (dateSuffix + output.Trim().PadLeft(6, '0'));
        }
    }
}
