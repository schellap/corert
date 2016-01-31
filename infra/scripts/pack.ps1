param (
    [Parameter(Mandatory=$true)]
    [string]$BuildOs,
    [Parameter(Mandatory=$true)]
    [string]$BuildType,
    [Parameter(Mandatory=$true)]
    [string]$BuildArch,
    [Parameter(Mandatory=$true)]
    [string]$Milestone,
    [switch]$Clean
)

. "$PSScriptRoot/common.ps1"

function Main
{
    Write-Host $Packaging
    If (-Not (Test-Path($Packaging))) {
        Exit 1
    }
    Issue-Command "$Packaging -m $Milestone --os $BuildOs --type $BuildType --arch $BuildArch --root `"$ProjectRoot`""
    Exit $LastExitCode
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
