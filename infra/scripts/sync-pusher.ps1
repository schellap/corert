. "$PSScriptRoot/common.ps1"

. "$PSScriptRoot\install-cli.ps1" -installdir "$BinDir\tools"
. "$PSScriptRoot\build.ps1"
. "$PSScriptRoot\push.ps1" -BuildOs Windows_NT -BuildType Debug -BuildArch x64 -Milestone nightly -JsonOnly true
