#!/usr/bin/env bash

__script_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
__clean=0
while [ "$1" != "" ]; do
    lowerI="$(echo $1 | awk '{print tolower($0)}')"
    case $lowerI in
        -h|--help)
            usage
            return 1
            ;;
        --build-os)
            shift
            __BuildOs=$1
            ;;
        --build-arch)
            shift
            __BuildArch=$1
            ;;
        --clean) 
            shift
            __clean=1
        ;;
    esac
    shift
done

__build_arch_lowcase=$(echo "${__BuildArch}" | tr '[:upper:]' '[:lower:]')
__build_os_lowcase=$(echo "${__BuildOs}" | tr '[:upper:]' '[:lower:]')
if [ ${__build_os_lowcase} == "osx" ]; then
    __build_rid=osx.10.10
else
    __build_rid=ubuntu.14.04
fi

source ${__script_dir}/common.sh

if [ ! -f "${__Packaging}" ] || [ $__clean == 1 ]; then
    ${__DotNet} restore ${__ProjectRoot}/infra/packaging -s https://www.myget.org/F/dotnet-core/ -s https://www.myget.org/F/dotnet-corefxlab/ -s https://www.nuget.org/api/v2/
    ${__DotNet} publish ${__ProjectRoot}/infra/packaging -c Release -o ${__BinRoot}/infra/packaging --framework dnxcore50 --runtime ${__build_rid}-${__build_arch_lowcase}
fi
