#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

param (
    [string] $Time = "18:00"
)

$Date = Get-Date($Time)
Write-Host $Date.ToString("ddd MMM dd hh:mm yyyy")
# Invoke-Expression "git rev-list -n 1 --before $Date.
