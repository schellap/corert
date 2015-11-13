#!/usr/bin/env bash

usage()
{
	echo "$0 [arch: x86/x64/arm/arm64] [flavor: debug/release]"
	exit 1

}

for i in "$@"
	do
		lowerI="$(echo $i | awk '{print tolower($0)}')"
		case $lowerI in
		-?|-h|-help)
			usage
			exit 1
			;;
		x86)
			__CoreRT_BuildArch=x86
			;;
		x64)
			__CoreRT_BuildArch=x64
			;;
		arm)
			__CoreRT_BuildArch=arm
			;;
		arm64)
			__CoreRT_BuildArch=arm64
			;;
		dbg)
			__CoreRT_BuildType=Debug
			;;
		debug)
			__CoreRT_BuildType=Debug
			;;
		rel)
			__CoreRT_BuildType=Release
			;;
		release)
			__CoreRT_BuildType=Release
			;;
		*)
			;;
	esac
done
