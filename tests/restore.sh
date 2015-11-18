#!/usr/bin/env bash

usage()
{
    echo "Usage: $0 [dbg^|rel] [x86^|amd64^|arm] [-?]"
    exit -8
}


source $(cd "$(dirname "$0")"; pwd -P)/testenv.sh

if [ -z ${__CoreRT_BuildOS} ]; then
    __CoreRT_BuildOS=Linux
fi

if [ -z ${__CoreRT_BuildArch} ]; then
    echo "Set __CoreRT_BuildArch to x86/x64/arm/arm64"
    exit -1
fi

if [ -z ${__CoreRT_BuildType} ]; then
    echo "Set __CoreRT_BuildType to Debug or Release"
    exit -1
fi

__build_os_lowcase=$(echo "${__CoreRT_BuildOS}" | tr '[:upper:]' '[:lower:]')
if [ ${__build_os_lowcase} != "osx" ]; then
    __BuildRid=ubuntu.14.04
else
    __BuildRid=osx.10.10
fi
__CoreRT_ToolchainPkg=toolchain.${__BuildRid}-${__CoreRT_BuildArch}.Microsoft.DotNet.ILCompiler.Development
__CoreRT_ToolchainVer=1.0.2-prerelease-00001
__CoreRT_AppDepSdkPkg=toolchain.${__BuildRid}-${__CoreRT_BuildArch}.Microsoft.DotNet.AppDep
__CoreRT_AppDepSdkVer=1.0.2-prerelease-00002

__ScriptDir=$(cd "$(dirname "$0")"; pwd -P)
__BuildStr=${__CoreRT_BuildOS}.${__CoreRT_BuildArch}.${__CoreRT_BuildType}

while test $# -gt 0
    do
        lowerI="$(echo $1 | awk '{print tolower($0)}')"
        case $lowerI in
        -?|-h|-help)
            usage
            exit 1
            ;;
        -nugetexedir)
            shift
            __NuGetExeDir=$1
            ;;
        -nupkgdir)
            shift
            __BuiltNuPkgDir=$1
            ;;
        -installdir)
            shift
            __NuPkgInstallDir=$1
            ;;
        -nugetopt)
            shift
            __NuGetOptions=$1
            ;;
        *)
            ;;
        esac
    shift
done

if [ ! -f ${__NuGetExeDir}/NuGet.exe ] ; then
    echo "No NuGet.exe found at ${__NuGetExeDir}.  Specify -nugetexedir option"
    exit -1
fi

if [ -z ${__NuPkgInstallDir} ] ; then
    echo "Specify -installdir option"
    exit -1
fi

if [ ! -d ${__BuiltNuPkgDir} ] ; then
    echo "Specify -nupkgdir to point to the build toolchain path"
    echo ${__BuiltNuPkgDir}
    exit -1
fi

echo "Cleaning up ${__NuPkgInstallDir}"
rm -rf ${__NuPkgInstallDir}
mkdir -p ${__NuPkgInstallDir}
if [ ! -d ${__NuPkgInstallDir} ]; then
    echo "Could not make install dir"
    exit -1
fi

__NuGetFeedUrl="https://www.myget.org/F/dotnet/auth/3e4f1dbe-f43a-45a8-b029-3ad4d25605ac/api/v2"

echo Installing CoreRT external dependencies
mono ${__NuGetExeDir}/NuGet.exe install -Source ${__NuGetFeedUrl} -OutputDir ${__NuPkgInstallDir} -Version ${__CoreRT_AppDepSdkVer} ${__CoreRT_AppDepSdkPkg} -prerelease ${__NuGetOptions} -nocache

__BuiltNuPkgPath=${__BuiltNuPkgDir}/${__CoreRT_ToolchainPkg}.${__CoreRT_ToolchainVer}.nupkg
echo Installing ILCompiler from ${__BuiltNuPkgPath} into ${__NuPkgInstallDir}

if [ ! -f ${__BuiltNuPkgPath} ]; then
    echo "Did not find a build ${__BuiltNuPkgPath}.  Did you run build.sh?"
    exit -1
fi

mono ${__NuGetExeDir}/NuGet.exe install -Source "${__BuiltNuPkgDir}" -OutputDir "${__NuPkgInstallDir}" ${__CoreRT_ToolchainPkg} -Version ${__CoreRT_ToolchainVer} -prerelease ${__NuGetOptions}
chmod +x ${__NuPkgInstallDir}/${__CoreRT_ToolchainPkg}.${__CoreRT_ToolchainVer}/corerun

export __CoreRT_AppDepSdkDir=${__NuPkgInstallDir}/${__CoreRT_AppDepSdkPkg}.${__CoreRT_AppDepSdkVer}
export __CoreRT_ToolchainDir=${__NuPkgInstallDir}/${__CoreRT_ToolchainPkg}.${__CoreRT_ToolchainVer}
export __CoreRT_RyuJitDir=${__CoreRT_ToolchainDir}
export __CoreRT_ObjWriterDir=${__CoreRT_ToolchainDir}
