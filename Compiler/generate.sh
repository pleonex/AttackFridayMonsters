#!/bin/bash
REPO_DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )/..
pushd "$REPO_DIR"

# Remove old build
rm -rf Compiler/temp Compiler/tools
mkdir Compiler/tools

# Compile 3dstool for ROM extracting / importing
cmake -HPrograms/ROMTool -BCompiler/temp
pushd Compiler/temp
make -j32  && mv "$REPO_DIR"/Programs/ROMTool/bin/Release/3dstool "$REPO_DIR"/Compiler/tools/
cp "$REPO_DIR"/Programs/ROMTool/bin/ignore_3dstool.txt "$REPO_DIR"/Compiler/tools/
popd
rm -rf Compiler/temp

popd
