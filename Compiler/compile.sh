#!/bin/bash
REPO_DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )/..

ROM_TOOL="$REPO_DIR"/Compiler/tools/3dstool
ROM_DIR="$REPO_DIR"/GameData/romdata
OUTPUT_FILE="$REPO_DIR"/GameData/Patched.3ds

# Remove old patch
rm "$OUTPUT_FILE"

# Generate ROM
echo "Packing game files"
"$ROM_TOOL" -ctf romfs "$ROM_DIR"/romfs.bin --romfs-dir "$ROM_DIR"/data

echo "Packing system files"
"$ROM_TOOL" -ctfz exefs "$ROM_DIR"/exefs.bin \
    --exefs-dir "$ROM_DIR"/system \
    --header "$ROM_DIR"/internals/header_system.bin

echo "Creating game file"
"$ROM_TOOL" -ctf cxi "$ROM_DIR"/game.bin \
    --exefs "$ROM_DIR"/exefs.bin \
    --romfs "$ROM_DIR"/romfs.bin \
    --header "$ROM_DIR"/internals/header_ncch0.bin \
    --exh "$ROM_DIR"/internals/exheader_ncch0.bin \
    --plain "$ROM_DIR"/internals/plain.bin

echo "Creating 3DS file"
"$ROM_TOOL" -ct01f cci \
    "$ROM_DIR"/game.bin "$ROM_DIR"/manual.bin \
    "$OUTPUT_FILE" \
    --header "$ROM_DIR"/internals/header_cci.bin

rm "$ROM_DIR"/romfs.bin "$ROM_DIR"/exefs.bin "$ROM_DIR"/game.bin
