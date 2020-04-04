#!/bin/bash

# Quit if any command throw errors
set -e

# Extract
# GameData/tools/3dstool -x -t cxi \
#     -f GameData/rom_metadata/program.bin \
#     --exh GameData/rom_metadata/exheader_ncc0.bin \
#     --plain GameData/rom_metadata/plain.bin \
#     --header GameData/rom_metadata/header_ncch0.bin

echo "Generating NCCH"
rm -f GameData/output/program.bin
GameData/tools/3dstool -c -t cxi -f GameData/output/program.bin \
    --exefs GameData/output/game.3ds.exefs \
    --romfs GameData/output/game.3ds.romfs \
    --exh GameData/rom_metadata/exheader_ncch0.bin \
    --plain GameData/rom_metadata/plain.bin \
    --header GameData/rom_metadata/header_ncch0.bin

# Extract
# GameData/tools/3dstool -x -t cci \
#     -1 GameData/rom_metadata/manual.bin \
#     -f GameData/game.3ds \
#     --header GameData/rom_metadata/header_cci.bin

echo "Generating NCSD"
rm -f GameData/output/game_patched.3ds
GameData/tools/3dstool -c -t cci \
    -0 GameData/output/program.bin -1 GameData/rom_metadata/manual.bin \
    -f GameData/output/game_patched.3ds \
    --header GameData/rom_metadata/header_cci.bin

echo "Generating patch"
xdelta3 -e -f -S -v \
    -s GameData/game.3ds \
    GameData/output/game_patched.3ds \
    GameData/output/patch.xdelta
