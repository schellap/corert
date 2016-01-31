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
    Issue-Command "$Packaging pack -m $Milestone --os $BuildOs --type $BuildType --arch $BuildArch --root `"$ProjectRoot`" --json-only $JsonOnly"
    Issue-Command "$Packaging push -m $Milestone --os $BuildOs --type $BuildType --arch $BuildArch --root `"$ProjectRoot`" --json-only $JsonOnly"
}

function Issue-Command
{
    param(
        [string]$CmdString
    )
    $str = $(Get-Location -stack) + "$" + $CmdString
    Write-Host $str -Foreground Cyan
    Invoke-Expression $CmdString
    If ($LastExitCode -ne 0) {
        Exit 1
    }
}

Main
