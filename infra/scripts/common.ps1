$ProjectRoot = "$PSScriptRoot\..\.."
$DotNet = "$ProjectRoot\bin\tools\cli\bin\dotnet.exe"

function Ensure-Packaging
{
    $PackagingDir = "$ProjectRoot\infra\packaging"
    $Packaging = "$PackagingDir\bin\Release\dnxcore50\packaging.exe"
    If ((-Not (Test-Path $Packaging)) -Or $Clean) {
        Issue-Command "$DotNet restore `"$PackagingDir`""
        Issue-Command "$DotNet build `"$PackagingDir`" -c Release"
    }
    Return $Packaging
}
