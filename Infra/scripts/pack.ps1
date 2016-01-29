param (
    $BuildOs = "Windows_NT",
    $BuildType = "Debug",
    $BuildArch = "x64",
    $Milestone = "testing"
)

Source ./common.ps1

Push-Location "`"$ProjectRoot\infra\scripts\packaging`""

Invoke-Expression "`"$ProjectRoot\cli\bin\dotnet`" restore"
Invoke-Expression "`"$ProjectRoot\cli\bin\dotnet`" build -c Release"
Invoke-Expression "bin\Release\dnxcore50\packaging.exe -m $Milestone --os $BuildOs --type $BuildType --arch $BuildArch --root `"$ProjectRoot`""

Pop-Location
