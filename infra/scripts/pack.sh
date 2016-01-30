#!/usr/bin/env bash

usage()
{
    echo "Usage: $0 --build-os [OSX, Linux] --build-arch [x64, x86, arm , arm64] --install-dir [dir path to CLI install]"
}

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
        --build-type)
           shift
           __BuildType=$1
           ;;
        --milestone) 
            shift
            __Milestone=$1
        ;;
    esac
    shift
done

source  "./common.sh"

pack()
{
    local __script_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
    pushd ${__ProjectRoot}/infra/packaging

    ${__DotNet} restore
    ${__DotNet} build -c Release
    ./bin/Release/dnxcore50/packaging -m ${__Milestone} --os ${__BuildOs} --type ${__BuildType} --arch ${__BuildArch} --root "${__ProjectRoot}"

    popd
}

pack
