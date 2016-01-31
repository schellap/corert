__script_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
source ${__script_dir}/common.sh

if [ ! -f "${__Pacakaging}" ]; then
    ${__DotNet} restore ${__ProjectRoot}/infra/packaging -s https://www.myget.org/F/dotnet-core/ -s https://www.myget.org/F/dotnet-corefxlab/ -s https://www.nuget.org/api/v2/
    ${__DotNet} build ${__ProjectRoot}/infra/packaging -c Release -o ${__BinRoot}/infra/packaging
fi
