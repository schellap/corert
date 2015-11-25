#!/usr/bin/env bash
dir=`dirname $0`
file=`basename $dir`
$dir/bin/Debug/dnxcore50/native/$file
if [ $? == 100];
    echo pass
    exit 0
else
    echo fail
    exit 1
fi
