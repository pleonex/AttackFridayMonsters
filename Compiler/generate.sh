#!/bin/bash
REPO_DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )/..

TOOLS_DIR="$REPO_DIR"/Compiler/tools
TEMP_DIR="$REPO_DIR"/Compiler/temp

# Remove old build
rm -rf "$TEMP_DIR" "$TOOLS_DIR"
mkdir "$TOOLS_DIR"

# Compile 3dstool for ROM extracting / importing
cmake -H"$REPO_DIR"/Programs/ROMTool -B"$TEMP_DIR"
pushd "$TEMP_DIR"
make -j32  && mv "$REPO_DIR"/Programs/ROMTool/bin/Release/3dstool "$TOOLS_DIR"/
cp "$REPO_DIR"/Programs/ROMTool/bin/ignore_3dstool.txt "$TOOLS_DIR"/
popd
rm -rf "$TEMP_DIR"

# Attack of the Friday Monsters formats
MSBUILD="msbuild /v:minimal /p:OutputPath=\"$TOOLS_DIR\" /p:Configuration=Release"
$MSBUILD "$REPO_DIR"/Programs/AttackFridayMonsters/AttackFridayMonsters.sln

# Compile LZX CUE tool
x86_64-w64-mingw32-gcc -o "$TOOLS_DIR"/lzx.exe "$REPO_DIR"/Programs/CUETools/lzx.c

# Font program
cp "$REPO_DIR"/Programs/FontTool/bcfnt.py "$TOOLS_DIR"/

# Remove debug symbols
rm "$TOOLS_DIR"/*.pdb
