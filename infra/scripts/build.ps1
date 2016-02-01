param (
     [switch]$Clean
)

. "$PSScriptRoot/common.ps1"

If ($Clean -Or (-Not (Test-Path "$Packaging"))) {
    Invoke-Expression "$DotNet restore `"$ProjectRoot\infra\packaging`" -s https://www.myget.org/F/dotnet-core/ -s https://www.myget.org/F/dotnet-corefxlab/ -s https://www.nuget.org/api/v2/"
    If ($LastExitCode -ne 0) { Exit 1 }
    Invoke-Expression "$DotNet publish `"$ProjectRoot\infra\packaging`" -c Release -o `"$BinRoot\infra\packaging`" -f dnxcore50 -r win7-x64"
    If ($LastExitCode -ne 0) { Exit 2 }
}
