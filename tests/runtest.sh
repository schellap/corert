#!/usr/bin/env bash

usage()
{
	echo "Usage: $0 [OS] [arch] [flavor] [-extrepo] [-buildextrepo] [-mode] [-runtest]"
	echo "    -mode         : Compilation mode. Specify cpp/protojit. Default: protojit"
	echo "    -runtest      : Should just compile or run compiled bianry? Specify: true/false. Default: true."
	echo "    -extrepo      : Path to external repo, currently supports: GitHub: dotnet/coreclr. Specify full path. If unspecified, runs corert tests"
	echo "    -buildextrepo : Should build at root level of external repo? Specify: true/false. Default: true"
	echo "    -nocache      : When restoring toolchain packages, obtain them from the feed not the cache."
	exit 1
}

compiletest()
{
    echo "Compiling test $1 $2"
    __SourceFolder=$1
    __SourceFileName=$2
    __SourceFile=${__SourceFolder}/${__SourceFileName}
    rm -f ${__SourceFile}.S
    rm -f ${__SourceFile}.compiled
    rm -f ${__SourceFile}.o
    rm -f ${__SourceFile}.exe

    echo Begin managed build of ${__SourceFile}.cs
    mcs -nologo -noconfig -unsafe+ -nowarn:1701,1702 -langversion:5 -nostdlib+ -errorreport:prompt -warn:4 -define:TRACE -define:DEBUG -define:SIGNED -reference:../packages/System.Collections/4.0.0/ref/dotnet/System.Collections.dll -reference:../packages/System.Console/4.0.0-beta-23419/ref/dotnet/System.Console.dll -reference:../packages/System.Diagnostics.Debug/4.0.0/ref/dotnet/System.Diagnostics.Debug.dll -reference:../packages/System.Globalization/4.0.0/ref/dotnet/System.Globalization.dll -reference:../packages/System.IO/4.0.10/ref/dotnet/System.IO.dll -reference:../packages/System.IO.FileSystem/4.0.0/ref/dotnet/System.IO.FileSystem.dll -reference:../packages/System.IO.FileSystem.Primitives/4.0.0/ref/dotnet/System.IO.FileSystem.Primitives.dll -reference:../packages/System.Reflection/4.0.0/ref/dotnet/System.Reflection.dll -reference:../packages/System.Reflection.Extensions/4.0.0/ref/dotnet/System.Reflection.Extensions.dll -reference:../packages/System.Reflection.Primitives/4.0.0/ref/dotnet/System.Reflection.Primitives.dll -reference:../packages/System.Resources.ResourceManager/4.0.0/ref/dotnet/System.Resources.ResourceManager.dll -reference:../packages/System.Runtime/4.0.20/ref/dotnet/System.Runtime.dll -reference:../packages/System.Runtime.Extensions/4.0.10/ref/dotnet/System.Runtime.Extensions.dll -reference:../packages/System.Runtime.Handles/4.0.0/ref/dotnet/System.Runtime.Handles.dll -reference:../packages/System.Runtime.InteropServices/4.0.10/ref/dotnet/System.Runtime.InteropServices.dll -reference:../packages/System.Text.Encoding/4.0.0/ref/dotnet/System.Text.Encoding.dll -reference:../packages/System.Text.Encoding.Extensions/4.0.0/ref/dotnet/System.Text.Encoding.Extensions.dll -reference:../packages/System.Threading/4.0.0/ref/dotnet/System.Threading.dll -reference:../packages/System.Threading.Overlapped/4.0.0/ref/dotnet/System.Threading.Overlapped.dll -reference:../packages/System.Threading.Tasks/4.0.10/ref/dotnet/System.Threading.Tasks.dll -debug+ -debug:full -filealign:512 -optimize- -out:${__SourceFile}.exe -target:exe -warnaserror+ ${__SourceFile}.cs
    echo Compiling ILToNative ${__SourceFile}.exe
    # hack
    chmod +x ${__CoreRT_ToolchainDir}/dotnet-compile-native.sh
    ${__CoreRT_ToolchainDir}/dotnet-compile-native.sh ${__BuildArch} ${__BuildType} -mode ${__CoreRT_TestCompileMode} -appdepsdk ${__CoreRT_AppDepSdkDir} -codegenpath ${__CoreRT_ProtoJitDir} -objgenpath ${__CoreRT_ObjWrtierDier} -logpath ${__CompileLogPath} -in ${__SourceFile}.exe -out ${__SourceFile}.compiled
}

__CoreRT_TestRoot=$(cd "$(dirname "$0")"; pwd -P)
__CoreRT_BuildArch=x64
__CoreRT_BuildType=Debug
__CoreRT_TestRun=true
__CoreRT_TestCompileMode=protojit
__CoreRT_TestExtRepo=
__CoreRT_BuildExtRepo=

# Use uname to determine what the OS is.
OSName=$(uname -s)
case $OSName in
    Linux)
        __CoreRT_BuildOS=Linux
        ;;

    Darwin)
        __CoreRT_BuildOS=OSX
        ;;

    FreeBSD)
        __CoreRT_BuildOS=FreeBSD
        ;;

    *)
        echo "Unsupported OS $OSName detected, configuring as if for Linux"
        __CoreRT_BuildOS=Linux
        ;;
esac

for i in "$@"
	do
		lowerI="$(echo $i | awk '{print tolower($0)}')"
		case $lowerI in
		-?|-h|--help)
			usage
			exit 1
			;;
		x86)
			__CoreRT_BuildArch=x86
			;;
		x64)
			__CoreRT_BuildArch=x64
			;;
		arm)
			__CoreRT_BuildArch=arm
			;;
		arm64)
			__CoreRT_BuildArch=arm64
			;;
		debug)
			__CoreRT_BuildType=Debug
			;;
		release)
			__CoreRT_BuildType=release
			;;
		-extrepo)
            shift
			__CoreRT_TestExtRepo=$i
			;;
		-mode)
            shift
			__CoreRT_TestCompileMode=$i
			;;
		-runtest)
            shift
			__CoreRT_TestRun=$i
			;;
		-nocache)
			__CoreRT_NuGetOptions=-nocache
			;;
		*)
			;;
	esac
done

__BuildStr=${__CoreRT_BuildOS}.${__CoreRT_BuildArch}.${__CoreRT_BuildType}
__BinDir=${__CoreRT_TestRoot}/../bin/tests
__LogDir=${__CoreRT_TestRoot}/../bin/Logs/${__BuildStr}/tests
__NuPkgInstallDir=${__BinDir}/package
__BuiltNuPkgDir=${__CoreRT_TestRoot}/../bin/Product/${__BuildStr}/.nuget

__PackageRestoreCmd=$__CoreRT_TestRoot/restore.sh
source ${__PackageRestoreCmd} -nugetexedir ${__CoreRT_TestRoot}/../packages -installdir ${__NuPkgInstallDir} -nupkgdir ${__BuiltNuPkgDir} -nugetopt ${__CoreRT_NuGetOptions}

if [ ! -d ${__CoreRT_AppDepSdkDir} ]; then
    echo "AppDep SDK not installed at ${__CoreRT_AppDepSdkDir}"
    exit -1
fi

if [ ! -d ${__CoreRT_ProtoJitDir} ]; then
    echo "ProtoJIT SDK not installed at ${__CoreRT_ProtoJitDir}"
    exit -1
fi

if [ ! -d ${__CoreRT_ObjWriterDir} ]; then
    echo "ObjWriter SDK not installed at ${__CoreRT_ObjWriterDir}"
    exit -1
fi

if [ ! -f ${__CoreRT_ToolchainDir}/dotnet-compile-native.sh ]; then
    echo "dotnet-compile-native.sh not found in ${__CoreRT_ToolchainDir}"
    exit -1
fi

if [ -z ${__CoreRT_ToolchainPkg} ]; then
    echo "Run ${__PackageRestoreCmd} first"
    exit -1
fi

if [ -z ${__CoreRT_ToolchainVer} ]; then
    echo "Run ${__PackageRestoreCmd} first"
    exit -1
fi

__TotalTests=0
__PassedTests=0

compiletest src/Simple/AsgAdd1 AsgAdd1

echo "TOTAL: ${__TotalTests} PASSED: ${__PassedTests}"

exit 0

#for /f "delims=" %%a in ('dir /s /aD /b src\*') do (
#    set __SourceFolder=%%a
#    set __SourceFileName=%%~na
#    set __RelativePath=!__SourceFolder:%__CoreRT_TestRoot%=!
#    if exist "!__SourceFolder!\!__SourceFileName!.cs" (
#        call :CompileFile !__SourceFolder! !__SourceFileName! %__LogDir%\!__RelativePath!
#        set /a __TotalTests=!__TotalTests!+1
#    ) else (echo !__SourceFolder!\!__SourceFileName!)
#)
#set /a __FailedTests=%__TotalTests%-%__PassedTests%



