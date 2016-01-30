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
    Push-Location "$ProjectRoot\infra\packaging"

    Issue-Command "$DotNet restore"
    Issue-Command "$DotNet build -c Release"
    Issue-Command "bin\Release\dnxcore50\packaging.exe push -m $Milestone --os $BuildOs --type $BuildType --arch $BuildArch --root `"$ProjectRoot`"" --json-only $JsonOnly

    Pop-Location
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
