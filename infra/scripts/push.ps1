param (
    [Parameter(Mandatory=$true)]
    [string]$BuildOs,
    [Parameter(Mandatory=$true)]
    [string]$BuildType,
    [Parameter(Mandatory=$true)]
    [string]$BuildArch,
    [Parameter(Mandatory=$true)]
    [string]$Milestone,
    [Parameter(Mandatory=$true)]
    [string]$JsonOnly
)

. "$PSScriptRoot/common.ps1"

function Main
{
    $Packaging = Ensure-Packaging
    Issue-Command "$Packaging push -m $Milestone --os $BuildOs --type $BuildType --arch $BuildArch --root `"$ProjectRoot`"" --json-only $JsonOnly
}

function Issue-Command
{
    param(
        [string]$CmdString
    )
    $str = $(Get-Location -stack) + "$" + $CmdString
    Write-Host $str -Foreground Cyan
    Invoke-Expression $CmdString
}

Main
