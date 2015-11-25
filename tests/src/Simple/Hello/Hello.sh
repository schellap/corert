#!/usr/bin/env bash
dir=`dirname $0`
file=`basename $dir`
if [[ $($dir/bin/Debug/dnxcore50/native/$file | tr -d '\r') = "Hello world" ]]; then
    echo pass
    exit 0
else
    echo fail
    exit 1
fi
