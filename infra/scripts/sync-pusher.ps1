. "$PSScriptRoot/common.ps1"

. "$PSScriptRoot\install-cli.ps1" -installdir "$BinRoot\tools"
. "$PSScriptRoot\build.ps1"
. "$PSScriptRoot\push.ps1" -BuildOs Windows_NT -BuildType Release -BuildArch x64 -Milestone nightly -JsonOnly true
