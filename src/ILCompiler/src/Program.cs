// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.CommandLine;
using System.Runtime.InteropServices;
using System.Linq;

using Internal.TypeSystem;

using Internal.CommandLine;

namespace ILCompiler
{
    internal class Program
    {
        private CompilationOptions _options;

        private Dictionary<string, string> _inputFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> _referenceFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private bool _help;
        private string _helpText;

        private Program()
        {
        }

        private void Help()
        {
            Console.WriteLine();
            Console.WriteLine("Microsoft .NET Native IL Compiler");
            Console.WriteLine(_helpText);
        }

        private void InitializeDefaultOptions()
        {
            _options = new CompilationOptions();

            _options.InputFilePaths = _inputFilePaths;
            _options.ReferenceFilePaths = _referenceFilePaths;

            _options.SystemModuleName = "System.Private.CoreLib";

#if FXCORE
            // We could offer this as a command line option, but then we also need to
            // load a different RyuJIT, so this is a future nice to have...
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                _options.TargetOS = TargetOS.Windows;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                _options.TargetOS = TargetOS.Linux;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                _options.TargetOS = TargetOS.OSX;
            else
                throw new NotImplementedException();
#else
            _options.TargetOS = TargetOS.Windows;
#endif

            _options.TargetArchitecture = TargetArchitecture.X64;
        }

        // TODO: Use System.CommandLine for command line parsing
        // https://github.com/dotnet/corert/issues/568
        private void ParseCommandLine(string[] args)
        {
            IReadOnlyList<string> explicitInputFiles = Array.Empty<string>();
            IReadOnlyList<string> implicitInputFiles = Array.Empty<string>();
            IReadOnlyList<string> referenceFiles = Array.Empty<string>();

            AssemblyName name = typeof(Program).GetTypeInfo().Assembly.GetName();
            ArgumentSyntax.Parse(args, syntax =>
            {
                syntax.ApplicationName = name.Name + " " + name.Version.ToString();

                syntax.HandleHelp = false;
                syntax.HandleErrors = false;
            
                syntax.DefineOption("h|help", ref _help, "Help message for ILC");
                syntax.DefineOptionList("i|in", ref explicitInputFiles, "Input file(s) to compile");
                syntax.DefineOptionList("r|reference", ref referenceFiles, "Reference file(s) for compilation");
                syntax.DefineOption("o|out", ref _options.OutputFilePath, "Output file for compilation");
                syntax.DefineOption("cpp", ref _options.IsCppCodeGen, "Compile for C++ code-generation");
                syntax.DefineOption("nolinenumbers", ref _options.NoLineNumbers, "Debug line numbers for C++ code-generation");
                syntax.DefineOption("dgmllog", ref _options.DgmlLog, "Write DGML log");
                syntax.DefineOption("fulllog", ref _options.FullLog, "Write full log");
                syntax.DefineOption("verbose", ref _options.Verbose, "Verbosity level for compilation");
                syntax.DefineOption("systemmodule", ref _options.SystemModuleName, "Custom system library implementation for ILC");
                syntax.DefineParameterList("in", ref implicitInputFiles, "Input file(s) to compile");

                _helpText = syntax.GetHelpText(Console.WindowWidth - 2);
            });
            foreach (var input in explicitInputFiles.Concat(implicitInputFiles))
                Helpers.AppendExpandedPaths(_inputFilePaths, input, true);

            foreach (var reference in referenceFiles)
                Helpers.AppendExpandedPaths(_referenceFilePaths, reference, true);
        }

        private void SingleFileCompilation()
        {
            Compilation compilation = new Compilation(_options);
            compilation.Log = _options.Verbose ? Console.Out : TextWriter.Null;

            compilation.CompileSingleFile();
        }

        private int Run(string[] args)
        {
            InitializeDefaultOptions();

            ParseCommandLine(args);
            if (_help)
            {
                Help();
                return 1;
            }

            if (_options.InputFilePaths.Count == 0)
                throw new CommandLineException("No input files specified");

            if (_options.OutputFilePath == null)
                throw new CommandLineException("Output filename must be specified (/out <file>)");

            // For now, we can do single file compilation only
            // TODO: Multifile
            SingleFileCompilation();

            return 0;
        }

        private static int Main(string[] args)
        {
#if DEBUG
            return new Program().Run(args);
#else
            try
            {
                return new Program().Run(args);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error: " + e.Message);
                return 1;
            }
#endif
        }
    }
}
