#!/usr/bin/env bash
$1/bin/Debug/dnxcore50/native/$2
if [ $? == 100 ]; then
    echo pass
    exit 0
else
    echo fail
    exit 1
fi
