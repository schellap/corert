. "$PSScriptRoot/common.ps1"

If (-Not (Test-Path "$Packaging")) {
    Invoke-Expression "$DotNet restore `"$ProjectRoot\infra\packaging`" -s https://www.myget.org/F/dotnet-core/ -s https://www.myget.org/F/dotnet-corefxlab/ -s https://www.nuget.org/api/v2/"
    If ($LastExitCode -ne 0) { Exit 1 }
    Invoke-Expression "$DotNet build `"$ProjectRoot\infra\packaging`" -c Release -o `"$BinRoot\infra\packaging`""
    If ($LastExitCode -ne 0) { Exit 1 }
}
