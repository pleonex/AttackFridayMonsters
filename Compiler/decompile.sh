#/bin/bash
REPO_DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )/..

TOOL="$REPO_DIR"/Compiler/tools/3dstool
OUTPUT_DIR="$REPO_DIR"/GameData/romdata
INPUT_FILE=$1

# Remove old directory
rm -rf "$OUTPUT_DIR"
mkdir "$OUTPUT_DIR"

# Extract
# 3ds extension are CCI files (gamecard dumps), containers of NCCH (aka CFA)
echo "Extracting 3DS file"
"$TOOL" -xt0f cci "$OUTPUT_DIR"/game.bin "$INPUT_FILE"

# Partition 0 is the game.
echo "Extracting game"
"$TOOL" -xtf cxi "$OUTPUT_DIR"/game.bin \
    --exefs "$OUTPUT_DIR"/exefs.bin \
    --romfs "$OUTPUT_DIR"/romfs.bin

"$TOOL" -xtfu exefs "$OUTPUT_DIR"/exefs.bin --exefs-dir "$OUTPUT_DIR"/system
"$TOOL" -xtf romfs "$OUTPUT_DIR"/romfs.bin --romfs-dir "$OUTPUT_DIR"/data

rm "$OUTPUT_DIR"/game.bin "$OUTPUT_DIR"/exefs.bin "$OUTPUT_DIR"/romfs.bin
