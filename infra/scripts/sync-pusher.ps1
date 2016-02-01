. "$PSScriptRoot\common.ps1"

. "$PSScriptRoot\install-cli.ps1" -installdir "$BinRoot\tools"
If ($? -ne 0) {
    Exit 1
}

. "$PSScriptRoot\build.ps1"
If ($? -ne 0) {
    Exit 2
}

. "$PSScriptRoot\push.ps1" -BuildOs Windows_NT -BuildType Release -BuildArch x64 -Milestone nightly -JsonOnly true
If ($? -ne 0) {
    Exit 3
}
