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
        --install-dir) 
            shift
            __CliDir=$1
        ;;
    esac
    shift
done

install()
{
    local __cli_dir=${__CliDir}
    if [ ! -d "${__cli_dir}" ]; then
        mkdir -p "${__cli_dir}"
    fi
    if [ ! -f "${__cli_dir}/bin/dotnet" ]; then
        local __build_os_lowercase=$(echo "${__BuildOS}" | tr '[:upper:]' '[:lower:]')

        # For Linux, we currently only support Ubuntu.
        if [ "${__build_os_lowercase}" == "linux" ]; then
            __build_os_lowercase="ubuntu"
        fi
        
        local __cli_version=latest
        local __cli_version_uri_part=$(echo ${__cli_version} | sed -e 's/^[a-z]/\u&/')
        local __build_arch_lowercase=$(echo "${__BuildArch}" | tr '[:upper:]' '[:lower:]')
        local __cli_tarball=dotnet-${__build_os_lowercase}-${__build_arch_lowercase}.${__cli_version}.tar.gz
        local __cli_tarball_path=${__tools_dir}/${__cli_tarball}
        download_file ${__cli_tarball_path} "https://dotnetcli.blob.core.windows.net/dotnet/dev/Binaries/${__cli_version_uri_part}/${__cli_tarball}"
        tar -xzf ${__cli_tarball_path} -C ${__cli_dir}
        export DOTNET_HOME=${__cli_dir}
        #
        # Workaround: Setting "HOME" for now to a dir in repo, as "dotnet restore"
        # depends on "HOME" to be set for its .dnx cache.
        #
        # See https://github.com/dotnet/cli/blob/5f5e3ad74c0c1de7071ba1309dca2ea289691163/scripts/ci_build.sh#L24
        #     https://github.com/dotnet/cli/issues/354
        #
        if [ -n ${HOME:+1} ]; then
            export HOME=${__tools_dir}
        fi
    fi
    
    if [ ! -f "${__cli_dir}/bin/dotnet" ]; then
        echo "CLI could not be installed or not present."
        return 1
    fi
    return 0
}

install
