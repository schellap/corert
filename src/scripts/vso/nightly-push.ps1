#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#


param (
    [string] $NuPkgDir = "",
    [string] $NuGetExe = "",
    [string] $NuGetSrc = "",
    [string] $NuGetAuth = "",
    [string] $Configuration = "",
    [string] $Version = ""
)


function main()
{
    $Microsoft_DotNet_ILCompiler = "Microsoft.DotNet.ILCompiler"
    $Microsoft_DotNet_ILCompiler_SDK = $Microsoft_DotNet_ILCompiler + ".SDK"
    
    $ListGrepStr = $Microsoft_DotNet_ILCompiler
    $RootPackages = @(
        $Microsoft_DotNet_ILCompiler,
        $Microsoft_DotNet_ILCompiler_SDK
    )
    
    $Rids = @(
        "win7-x64",
        "ubuntu.14.04-x64",
        "osx.10.10-x64"
    )
    
    # Get the package name strings
    $PackageGrepStr = @()
    for ($i = 0; $i -lt $Rids.length; $i++) {
        for ($j=0; $j -lt $RootPackages.length; $j++) {
        	$PackageGrepStr += "toolchain." + $Rids[$i] + "." + $RootPackages[$j] + " " + $Version
        }
    }
    
    $MaxAttempts = 3
    $PushedPackages = $False
    $ExpectedMatches = $PackageGrepStr.length
    $AttemptAfterSec = 300
    $Attempt = 0
    While ($True) {
        $Attempt++

        # Attempt to find all packages
        $NuGetOutput = Invoke-Expression "$NuGetExe list -Source $NuGetSrc $ListGrepStr -PreRelease"
        if ($LastExitCode -ne 0) {
            Write-Host "Error: nuget list $ListGrepStr"
            Throw
        }
    
        # Compare the name strings with nuget list output
        $TotalMatches = 0
        for ($i = 0; $i -lt $PackageGrepStr.length; $i++) {
            $count = ([regex]::Matches($NuGetOutput, $PackageGrepStr[$i])).count
            if ($count -eq 0) {
                Write-Host "Package not found in feed: " $PackageGrepStr[$i] -ForeGroundColor Red
            }
            $TotalMatches += $count;
        }
    
        If ($TotalMatches -eq $ExpectedMatches) {
            Push-Packages -PushPackages $RootPackages -NuPkgDir $NuPkgDir -Version $Version
            $PushedPackages = $True
            Write-Host "Packages $(RootPackages.Length) pushed successfully!"
            Break
        } ElseIf ($Attempt -lt $MaxAttempts) {
            Write-Host "Attempt $Attempt/$MaxAttempts couldn't find all packages. Will retry after $AttemptAfterSec seconds..."
            Start-Sleep -s $AttemptAfterSec
        } Else {
            Write-Host "All the $MaxAttempts attempts failed to find all packages."
            Break
        }
    }
    
    If ($PushedPackages -ne $True) {
        Write-Host "Error: Not all platform packages were found in the feed (actual: $TotalMatches, expected: $ExpectedMatches). Will not push root packages." -BackgroundColor Red
        Throw
    }
}

#------------------------------------------------------------------------
function Push-Packages {
    param(
        [string[]] $PushPackages = @(),
        [string] $NuPkgDir = "",
        [string] $Version = ""
    )
    for ($j=0; $j -lt $PushPackages.length; $j++) {
        $command = "$NuGetExe push `"$NuPkgDir" + $PushPackages[$j] + ".$Version.nupkg`" $NuGetAuth -Source $NuGetSrc"
        Write-Host $command
        Invoke-Expression $command
        if ($LastExitCode -ne 0) {
            Write-Host "Error: nuget push" -ForeGroundColor Red
            Throw
        }
    }
}

#------------------------------------------------------------------------
main
