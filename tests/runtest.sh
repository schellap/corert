#!/usr/bin/env bash

usage()
{
    echo "Usage: $0 [OS] [arch] [flavor] [-extrepo] [-buildextrepo] [-mode] [-runtest]"
    echo "    -mode         : Compilation mode. Specify cpp/ryujit. Default: ryujit"
    echo "    -runtest      : Should just compile or run compiled bianry? Specify: true/false. Default: true."
    echo "    -extrepo      : Path to external repo, currently supports: GitHub: dotnet/coreclr. Specify full path. If unspecified, runs corert tests"
    echo "    -buildextrepo : Should build at root level of external repo? Specify: true/false. Default: true"
    echo "    -nocache      : When restoring toolchain packages, obtain them from the feed not the cache."
    exit 1
}

runtest()
{
    echo "Running test $1 $2"
    __SourceFolder=$1
    __SourceFileName=$2
    __SourceFile=${__SourceFolder}/${__SourceFileName}
    ${__SourceFile}.sh $1 $2 ${__CoreRT_BuildType}
    return $?
}

restore()
{
    #${__CoreRT_TestRoot}/../packages/dnx-mono.1.0.0-beta8/bin/dnu restore $1
    ${__CoreRT_CliBinDir}/dotnet restore $1
}

compiletest()
{
    echo "Compiling dir $1 with dotnet compile $2"
    rm -rf $1/bin $1/obj
    ${__CoreRT_CliBinDir}/dotnet compile --native -c ${__CoreRT_BuildType} --ilcpath ${__CoreRT_ToolchainDir} $1 $2
}

run_test_dir()
{
    local __test_dir=$1
    local __restore=$2
    local __mode=$3
    local __dir_path=`dirname ${__test_dir}`
    local __filename=`basename ${__dir_path}`
    if [ ${__restore} == 1 ]; then
      restore ${__dir_path}
    fi
    local __compile_args=""
    if [ "${__mode}" = "Cpp" ]; then
      __compile_args="--cpp"
    fi
    compiletest ${__dir_path} ${__compile_args}
    runtest ${__dir_path} ${__filename}
    local __exitcode=$?
    if [ ${__exitcode} == 0 ]; then
        local __pass_var=__${__mode}PassedTests
        eval ${__pass_var}=$((${__pass_var} + 1))
        echo "<test name=\"${__dir_path}\" type=\"${__filename}:${__mode}\" method=\"Main\" result=\"Pass\" />" >> ${__TestBinDir}/testResults.tmp
    else
        echo "<test name=\"${__dir_path}\" type=\"${__filename}:${__mode}\" method=\"Main\" result=\"Fail\" />" >> ${__TestBinDir}/testResults.tmp
        echo "<failure exception-type=\"Exit code: ${__exitcode}\">" >> ${__TestBinDir}/testResults.tmp
        echo     "<message>See ${__dir_path} /bin or /obj for logs </message>" >> ${__TestBinDir}/testResults.tmp
        echo "</failure>" >> ${__TestBinDir}/testResults.tmp
        echo "</test>" >> ${__TestBinDir}/testResults.tmp
    fi
    local __total_var=__${__mode}TotalTests
    eval ${__total_var}=$((${__total_var} + 1))
    return $?
}

__CoreRT_TestRoot=$(cd "$(dirname "$0")"; pwd -P)
__CoreRT_CliBinDir=${__CoreRT_TestRoot}/../bin/tools/cli/bin
__CoreRT_BuildArch=x64
__CoreRT_BuildType=Debug
__CoreRT_TestRun=true
__CoreRT_TestCompileMode=ryujit
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
            __CoreRT_BuildType=Release
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
__TestBinDir=${__CoreRT_TestRoot}/../bin/tests
__LogDir=${__CoreRT_TestRoot}/../bin/Logs/${__BuildStr}/tests
__NuPkgInstallDir=${__TestBinDir}/package
__BuiltNuPkgDir=${__CoreRT_TestRoot}/../bin/Product/${__BuildStr}/.nuget
__PackageRestoreCmd=$__CoreRT_TestRoot/restore.sh
source ${__PackageRestoreCmd} -nugetexedir ${__CoreRT_TestRoot}/../packages -installdir ${__NuPkgInstallDir} -nupkgdir ${__BuiltNuPkgDir} -nugetopt ${__CoreRT_NuGetOptions}

if [ ! -d ${__CoreRT_AppDepSdkDir} ]; then
    echo "AppDep SDK not installed at ${__CoreRT_AppDepSdkDir}"
    exit -1
fi

if [ ! -d ${__CoreRT_ToolchainDir} ]; then
    echo "Toolchain not found in ${__CoreRT_ToolchainDir}"
    exit -1
fi

__CppTotalTests=0
__CppPassedTests=0
__JitTotalTests=0
__JitPassedTests=0

echo > ${__TestBinDir}/testResults.tmp

__BuildOsLowcase=$(echo "${__CoreRT_BuildOS}" | tr '[:upper:]' '[:lower:]')

for json in $(find src -iname 'project.json')
do
    __restore=1
    # Disable RyuJIT for OSX.
    if [ ${__BuildOsLowcase} != "osx" ]; then
        run_test_dir ${json} ${__restore} "Jit"
        __restore=0
    fi
    run_test_dir ${json} ${__restore} "Cpp"
done

__TotalTests=$((${__JitTotalTests} + ${__CppTotalTests}))
__PassedTests=$((${__JitPassedTests} + ${__CppPassedTests}))
__FailedTests=$((${__TotalTests} - ${__PassedTests}))

echo "<?xml version=\"1.0\" encoding=\"utf-8\"?^>" > ${__TestBinDir}/testResults.xml
echo "<assemblies>"  >> ${__TestBinDir}/testResults.xml
echo "<assembly name=\"ILCompiler\" total=\"${__TotalTests}\" passed=\"${__PassedTests}\" failed=\"${__FailedTests}\" skipped=\"0\">"  >> ${__TestBinDir}/testResults.xml
echo "<collection total=\"${__TotalTests}\" passed=\"${__PassedTests}\" failed=\"${__FailedTests}\" skipped=\"0\">"  >> ${__TestBinDir}/testResults.xml
cat "${__TestBinDir}/testResults.tmp" >> ${__TestBinDir}/testResults.xml
echo "</collection>"  >> ${__TestBinDir}/testResults.xml
echo "</assembly>"  >> ${__TestBinDir}/testResults.xml
echo "</assemblies>"  >> ${__TestBinDir}/testResults.xml


echo "JIT - TOTAL: ${__JitTotalTests} PASSED: ${__JitPassedTests}"
echo "CPP - TOTAL: ${__CppTotalTests} PASSED: ${__CppPassedTests}"

# Disable RyuJIT for OSX.
if [ ${__BuildOsLowcase} != "osx" ]; then
    if [ ${__JitTotalTests} == 0 ]; then
        exit 1
    fi
fi

if [ ${__CppTotalTests} == 0 ]; then
    exit 1
fi
if [ ${__JitTotalTests} -gt ${__JitPassedTests} ]; then
    exit 1
fi
if [ ${__CppTotalTests} -gt ${__CppPassedTests} ]; then
    exit 1
fi

exit 0


