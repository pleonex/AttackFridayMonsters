#/bin/bash
# ARGUMENTS:
#  $1: Input ROM
REPO_DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )/..

INPUT_FILE=$1
TEMP_DIR="$REPO_DIR"/Compiler/temp
TOOLS_DIR="$REPO_DIR"/Compiler/tools
ROM_DIR="$REPO_DIR"/GameData/rom
MOD_DIR="$REPO_DIR"/GameData/mod

# Remove old directory
rm -rf "$ROM_DIR"; mkdir "$ROM_DIR"
rm -rf "$MOD_DIR"; mkdir "$MOD_DIR"
rm -rf "$TEMP_DIR"; mkdir "$TEMP_DIR"

# Extract files
ROM_TOOL="$TOOLS_DIR"/3dstool

# 3ds extension are CCI files (gamecard dumps), containers of NCCH (aka CFA)
echo "Extracting 3DS file"
mkdir "$ROM_DIR"/internals
"$ROM_TOOL" -xt01f cci \
    "$ROM_DIR"/game.bin "$ROM_DIR"/manual.bin \
    "$INPUT_FILE" \
    --header "$ROM_DIR"/internals/header_cci.bin

# Partition 0 is the game.
echo "Extracting game"
"$ROM_TOOL" -xtf cxi "$ROM_DIR"/game.bin \
    --exefs "$ROM_DIR"/exefs.bin \
    --romfs "$ROM_DIR"/romfs.bin \
    --header "$ROM_DIR"/internals/header_ncch0.bin \
    --exh "$ROM_DIR"/internals/exheader_ncch0.bin \
    --plain "$ROM_DIR"/internals/plain.bin

echo "... Extracting ROM files"
"$ROM_TOOL" -xtf romfs "$ROM_DIR"/romfs.bin --romfs-dir "$ROM_DIR"/data

echo "... Extracting system files"
"$ROM_TOOL" -xtfu exefs "$ROM_DIR"/exefs.bin \
    --exefs-dir "$ROM_DIR"/system \
    --header "$ROM_DIR"/internals/header_system.bin

# Remove extracted files
rm "$ROM_DIR"/game.bin "$ROM_DIR"/exefs.bin "$ROM_DIR"/romfs.bin


# Convert game files
## Fonts
echo "Converting fonts"
mkdir "$MOD_DIR"/Font && pushd "$MOD_DIR"/Font >& /dev/null
mono "$TOOLS_DIR"/AttackFridayMonsters.exe -e darc \
    "$ROM_DIR"/data/gkk/lyt/title.arc \
    "$TEMP_DIR"/title
python "$TOOLS_DIR"/bcfnt.py -x -f "$TEMP_DIR"/title/font/kk_KN_Font.bcfnt
popd >& /dev/null


## Texts
echo "Converting texts"
mkdir "$MOD_DIR"/Texts

### Small texts
mono "$TOOLS_DIR"/AttackFridayMonsters.exe -e carddata0 \
    "$ROM_DIR"/data/gkk/cardgame/carddata.bin \
    "$MOD_DIR"/Texts/cardinfo.po

mono "$TOOLS_DIR"/AttackFridayMonsters.exe -e carddata25 \
    "$ROM_DIR"/data/gkk/cardgame/carddata.bin \
    "$MOD_DIR"/Texts/cardgame_dialogs.po

mono "$TOOLS_DIR"/AttackFridayMonsters.exe -e episode \
    "$ROM_DIR"/data/gkk/episode/episode.bin \
    "$MOD_DIR"/Texts/episode_titles.po

### Scripts
echo "... Converting scripts"
mkdir "$MOD_DIR"/Texts/Scripts
pushd "$TOOLS_DIR"
for MAP_FILE in "$ROM_DIR"/data/gkk/map_gz/*.lz; do
    MAP_NAME=`basename "${MAP_FILE%.*}"`
    mono "$TOOLS_DIR"/AttackFridayMonsters.exe -e script \
        "$MAP_FILE" \
        "$MOD_DIR"/Texts/Scripts/$MAP_NAME.po
done
popd

# Remove again temp dir
rm -rf "$TEMP_DIR"
