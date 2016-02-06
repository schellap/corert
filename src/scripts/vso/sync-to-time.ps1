#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

# Default to the current hour as the sync time or if a time is specified use that
# time of the current day. All are in Pacific Standard Time.
param (
    [string] $Time = "HH:00:00"
)

$PstZone = [System.TimeZoneInfo]::FindSystemTimeZoneById("Pacific Standard Time")
$PstTime = [System.TimeZoneInfo]::ConvertTimeFromUtc((Get-Date).ToUniversalTime(), $PstZone)

$FormatDt = "ddd MMM dd $Time yyyy -0800" # PST
$SyncTime = $PstTime.ToString($FormatDt)

$CurrentBranch=Invoke-Expression "git rev-parse --abbrev-ref HEAD"
$CommitHash=Invoke-Expression "git rev-list -n 1 --before=`"$SyncTime`" `"$CurrentBranch`""

Write-Host "Checking out to sync time: $SyncTime -> hash: $CommitHash"
Invoke-Expression "git checkout $CommitHash 2> checkout.log"
Write-Host "Exit code from git is $LastExitCode"
If ($LastExitCode -ne 0) {
    Write-Host "Exit code is non-zero, exiting..."
    Throw
}

Invoke-Expression "type checkout.log"
Exit 0
