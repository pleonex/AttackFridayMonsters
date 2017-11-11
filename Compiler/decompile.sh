#/bin/bash
# ARGUMENTS:
#  $1: Input ROM
REPO_DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )/..

ROM_TOOL="$REPO_DIR"/Compiler/tools/3dstool
OUTPUT_DIR="$REPO_DIR"/GameData/romdata
INPUT_FILE=$1

# Remove old directory
rm -rf "$OUTPUT_DIR"
mkdir "$OUTPUT_DIR"

# Extract
# 3ds extension are CCI files (gamecard dumps), containers of NCCH (aka CFA)
echo "Extracting 3DS file"
mkdir "$OUTPUT_DIR"/internals
"$ROM_TOOL" -xt01f cci \
    "$OUTPUT_DIR"/game.bin "$OUTPUT_DIR"/manual.bin \
    "$INPUT_FILE" \
    --header "$OUTPUT_DIR"/internals/header_cci.bin

# Partition 0 is the game.
echo "Extracting game"
"$ROM_TOOL" -xtf cxi "$OUTPUT_DIR"/game.bin \
    --exefs "$OUTPUT_DIR"/exefs.bin \
    --romfs "$OUTPUT_DIR"/romfs.bin \
    --header "$OUTPUT_DIR"/internals/header_ncch0.bin \
    --exh "$OUTPUT_DIR"/internals/exheader_ncch0.bin \
    --plain "$OUTPUT_DIR"/internals/plain.bin

echo "... Extracting ROM files"
"$ROM_TOOL" -xtf romfs "$OUTPUT_DIR"/romfs.bin --romfs-dir "$OUTPUT_DIR"/data

echo "... Extracting system files"
"$ROM_TOOL" -xtfu exefs "$OUTPUT_DIR"/exefs.bin \
    --exefs-dir "$OUTPUT_DIR"/system \
    --header "$OUTPUT_DIR"/internals/header_system.bin

# Remove extracted files
rm "$OUTPUT_DIR"/game.bin "$OUTPUT_DIR"/exefs.bin "$OUTPUT_DIR"/romfs.bin
