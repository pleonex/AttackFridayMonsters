#  Copyright (c) 2017 Benito Palacios Sanchez
#
#  This program is free software: you can redistribute it and/or modify
#  it under the terms of the GNU General Public License as published by
#  the Free Software Foundation, either version 3 of the License, or
#  (at your option) any later version.
#
#  This program is distributed in the hope that it will be useful,
#  but WITHOUT ANY WARRANTY; without even the implied warranty of
#  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
#  GNU General Public License for more details.
#
#  You should have received a copy of the GNU General Public License
#  along with this program.  If not, see <http://www.gnu.org/licenses/>.
cmake_minimum_required(VERSION 3.0)
project(AttackFridayMonstersDecompiler VERSION 1.0 LANGUAGES NONE)

# Options
set(DECOMPILER_ROM_FILE "${PROJECT_SOURCE_DIR}/../GameData/game.3ds" CACHE STRING "ROM file to decompile")
set(DECOMPILER_TOOLS_DIR "${PROJECT_SOURCE_DIR}/../GameData/tools" CACHE STRING "Decompilation tools")
set(DECOMPILER_PREFIX "${PROJECT_SOURCE_DIR}" CACHE STRING "Root directory for decompilation")
set(DECOMPILER_ROM_DIR "${DECOMPILER_PREFIX}/rom" CACHE STRING "Directory to store ROM files")
set(DECOMPILER_MOD_DIR "${DECOMPILER_PREFIX}/mod" CACHE STRING "Directory to store modification files")
set(DECOMPILER_ROM_INTERNAL_DIR "${DECOMPILER_ROM_DIR}/internal" CACHE STRING "Directory to store ROM internal data for compilation")

# Validation checks
if(NOT EXISTS "${DECOMPILER_ROM_FILE}")
    message(FATAL_ERROR "ROM file does not exist: ${DECOMPILER_ROM_FILE}")
endif()
if(NOT EXISTS "${DECOMPILER_TOOLS_DIR}")
    message(FATAL_ERROR "Missing tool binaries: ${DECOMPILER_TOOLS_DIR}")
endif()

# Summary
message(STATUS "## Project summary          ##")
message(STATUS "ROM file ....................: ${DECOMPILER_ROM_FILE}")
message(STATUS "Tools dir ...................: ${DECOMPILER_TOOLS_DIR}")
message(STATUS "Prefix path .................: ${DECOMPILER_PREFIX}")
message(STATUS "Output ROM dir ..............: ${DECOMPILER_ROM_DIR}")
message(STATUS "Output modifications dir.....: ${DECOMPILER_MOD_DIR}")
message(STATUS "Output ROM internals dir.....: ${DECOMPILER_ROM_INTERNAL_DIR}")
message(STATUS "##############################")

# Create mod folder structure
set(DECOMPILER_MOD_FONTS_DIR "${DECOMPILER_MOD_DIR}/Fonts")
set(DECOMPILER_MOD_TEXTS_DIR "${DECOMPILER_MOD_DIR}/Texts")
set(DECOMPILER_MOD_SCRIPTS_DIR "${DECOMPILER_MOD_TEXTS_DIR}/Scripts")
set(DECOMPILER_MOD_LAYOUT_DIR "${DECOMPILER_MOD_TEXTS_DIR}/Layouts")
set(DECOMPILER_MOD_IMGS_DIR "${DECOMPILER_MOD_DIR}/Images")
file(MAKE_DIRECTORY "${DECOMPILER_ROM_DIR}")
file(MAKE_DIRECTORY "${DECOMPILER_ROM_INTERNAL_DIR}")
file(MAKE_DIRECTORY "${DECOMPILER_MOD_DIR}")
file(MAKE_DIRECTORY "${DECOMPILER_MOD_FONTS_DIR}")
file(MAKE_DIRECTORY "${DECOMPILER_MOD_TEXTS_DIR}")
file(MAKE_DIRECTORY "${DECOMPILER_MOD_SCRIPTS_DIR}")
file(MAKE_DIRECTORY "${DECOMPILER_MOD_LAYOUT_DIR}")

# Find dependencies
list(APPEND CMAKE_MODULE_PATH "${PROJECT_SOURCE_DIR}/../CMakeModules")
set(CMAKE_PROGRAM_PATH "${DECOMPILER_TOOLS_DIR}")
include(3dsRoms)
include(3dsFonts)
include(3dsImages)
include(AttackFridayMonsters)

# 1. Extract ROM
extract_3ds_rom(
    ROM "${DECOMPILER_ROM_FILE}"
    ROM_DIR "${DECOMPILER_ROM_DIR}"
    INTERNAL_DIR "${DECOMPILER_ROM_INTERNAL_DIR}"
)

# 2. Convert font
extract_darc(
    NAME Title
    FILE "${DECOMPILER_ROM_DIR}/data/gkk/lyt/title.arc"
    OUTPUT "${CMAKE_BINARY_DIR}/title"
    DEPENDS Extract3DSROM
)
export_3ds_font(
    FONT "${CMAKE_BINARY_DIR}/title/font/kk_KN_Font.bcfnt"
    OUTPUT "${DECOMPILER_MOD_FONTS_DIR}"
    DEPENDS ExtractDarcTitle
)


# 3. Extract texts
# 3.1 Extract card related text
export_card_texts(
    FILE "${DECOMPILER_ROM_DIR}/data/gkk/cardgame/carddata.bin"
    OUTPUT "${DECOMPILER_MOD_TEXTS_DIR}"
)

# 3.2 Extract episode text
export_episodes_titles(
    FILE "${DECOMPILER_ROM_DIR}/data/gkk/episode/episode.bin"
    OUTPUT "${DECOMPILER_MOD_TEXTS_DIR}"
)

# 3.3 Extract binary text
# TODO

# 3.4 Extract scripts
export_scripts_text(
    OUTPUT "${DECOMPILER_MOD_SCRIPTS_DIR}"
    MAP_FILES
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A01000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A01100.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A03000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A03001.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A03100.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A03101.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A04000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A04100.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A05000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A05100.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A06000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A06100.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A07000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A08000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A08001.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A09000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A09100.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A10000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A10100.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A11000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A11001.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A12000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A13000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A13100.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A14000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A14100.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A15000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A16000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A18000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A18001.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A18100.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A18101.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A19000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A19100.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A20000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A21000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A21100.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A21101.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A21102.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A21103.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A21104.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A21105.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A21106.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A21107.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A21108.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A22000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A22100.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A22101.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A22102.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A22103.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A22104.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A22105.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A22106.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A23000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A25000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A25001.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A26000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A27000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A27001.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A27100.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A27101.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A28000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A29000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A29100.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A31000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A32000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A33000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A33100.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A34000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A34100.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A35000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A35100.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A36000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A36100.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A37000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A37100.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A38000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A98000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/A99000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/B02000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/B02001.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/B02100.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/B03001.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/B03002.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/B04000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/B04001.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/B04100.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/B05000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/B05100.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/B06000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/B06100.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/B07001.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/B07100.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/B07101.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/B08000.lz"
    "${DECOMPILER_ROM_DIR}/data/gkk/map_gz/B08100.lz"
)
# TODO: extract scripts from sysdata

# 4. Exract images
# Title (already extracted for fonts)
export_image_bclim(
    NAME Title
    OUTPUT "${DECOMPILER_MOD_IMGS_DIR}/title"
    DEPENDS ExtractDarcTitle
    BCLIM_FILES
    "${CMAKE_BINARY_DIR}/title/timg/auto_mk.bclim"
    "${CMAKE_BINARY_DIR}/title/timg/butji_mdr.bclim"
    "${CMAKE_BINARY_DIR}/title/timg/butji_n.bclim"
    "${CMAKE_BINARY_DIR}/title/timg/butji_y.bclim"
    "${CMAKE_BINARY_DIR}/title/timg/epi_moji.bclim"
    "${CMAKE_BINARY_DIR}/title/timg/head_load.bclim"
    "${CMAKE_BINARY_DIR}/title/timg/head_save.bclim"
    "${CMAKE_BINARY_DIR}/title/timg/title_rogo.bclim"
)

# Bumper
extract_darc(
    NAME Bumper
    FILE "${DECOMPILER_ROM_DIR}/data/gkk/lyt/bumper.arc"
    OUTPUT "${CMAKE_BINARY_DIR}/bumper"
    DEPENDS Extract3DSROM
)
export_image_bclim(
    NAME Bumper
    OUTPUT "${DECOMPILER_MOD_IMGS_DIR}/bumper"
    DEPENDS ExtractDarcBumper
    BCLIM_FILES
    "${CMAKE_BINARY_DIR}/bumper/timg/ex_msg_A.bclim"
    "${CMAKE_BINARY_DIR}/bumper/timg/ex_msg_B.bclim"
    "${CMAKE_BINARY_DIR}/bumper/timg/ex_msg_C.bclim"
    "${CMAKE_BINARY_DIR}/bumper/timg/stereo_ji.bclim"
)

# cardlyt_d
extract_darc(
    NAME Cardlyt
    FILE "${DECOMPILER_ROM_DIR}/data/gkk/cardgame/cardlyt_d.arc"
    OUTPUT "${CMAKE_BINARY_DIR}/cardlyt"
    DEPENDS Extract3DSROM
)
export_image_bclim(
    NAME Cardlyt
    OUTPUT "${DECOMPILER_MOD_IMGS_DIR}/cardlyt"
    DEPENDS ExtractDarcCardlyt
    BCLIM_FILES
    "${CMAKE_BINARY_DIR}/cardlyt/timg/butji_mdr.bclim"
    "${CMAKE_BINARY_DIR}/cardlyt/timg/butji_n.bclim"
    "${CMAKE_BINARY_DIR}/cardlyt/timg/butji_y.bclim"
    "${CMAKE_BINARY_DIR}/cardlyt/timg/chan_moji.bclim"
    "${CMAKE_BINARY_DIR}/cardlyt/timg/dasu_butt.bclim"
    "${CMAKE_BINARY_DIR}/cardlyt/timg/mai_1.bclim"
    "${CMAKE_BINARY_DIR}/cardlyt/timg/mai_2.bclim"
    "${CMAKE_BINARY_DIR}/cardlyt/timg/mai_3.bclim"
    "${CMAKE_BINARY_DIR}/cardlyt/timg/mai_4.bclim"
    "${CMAKE_BINARY_DIR}/cardlyt/timg/mai_5.bclim"
)

# movie_low
extract_darc(
    NAME Movielow
    FILE "${DECOMPILER_ROM_DIR}/data/gkk/lyt/movie_low.arc"
    OUTPUT "${CMAKE_BINARY_DIR}/movie_low"
    DEPENDS Extract3DSROM
)
export_image_bclim(
    NAME Movielow
    OUTPUT "${DECOMPILER_MOD_IMGS_DIR}/movie_low"
    DEPENDS ExtractDarcMovielow
    BCLIM_FILES
    "${CMAKE_BINARY_DIR}/movie_low/timg/skip_ji.bclim"
)

# notebook
extract_darc(
    NAME Notebook
    FILE "${DECOMPILER_ROM_DIR}/data/gkk/lyt/notebook.arc"
    OUTPUT "${CMAKE_BINARY_DIR}/notebook"
    DEPENDS Extract3DSROM
)
export_image_bclim(
    NAME Notebook
    OUTPUT "${DECOMPILER_MOD_IMGS_DIR}/notebook"
    DEPENDS ExtractDarcNotebook
    BCLIM_FILES
    "${CMAKE_BINARY_DIR}/notebook/timg/auto_moji.bclim"
    "${CMAKE_BINARY_DIR}/notebook/timg/butji_mdr.bclim"
    "${CMAKE_BINARY_DIR}/notebook/timg/page_00.bclim"
    "${CMAKE_BINARY_DIR}/notebook/timg/page_01.bclim"
    "${CMAKE_BINARY_DIR}/notebook/timg/page_02a.bclim"
    "${CMAKE_BINARY_DIR}/notebook/timg/page_02b.bclim"
    "${CMAKE_BINARY_DIR}/notebook/timg/page_03.bclim"
    "${CMAKE_BINARY_DIR}/notebook/timg/page_04.bclim"
    "${CMAKE_BINARY_DIR}/notebook/timg/page_05a.bclim"
    "${CMAKE_BINARY_DIR}/notebook/timg/page_05b.bclim"
    "${CMAKE_BINARY_DIR}/notebook/timg/page_06a.bclim"
    "${CMAKE_BINARY_DIR}/notebook/timg/page_06b.bclim"
    "${CMAKE_BINARY_DIR}/notebook/timg/page_07.bclim"
    "${CMAKE_BINARY_DIR}/notebook/timg/page_08.bclim"
    "${CMAKE_BINARY_DIR}/notebook/timg/page_09.bclim"
    "${CMAKE_BINARY_DIR}/notebook/timg/page_10.bclim"
    "${CMAKE_BINARY_DIR}/notebook/timg/page_11.bclim"
    "${CMAKE_BINARY_DIR}/notebook/timg/page_12.bclim"
    "${CMAKE_BINARY_DIR}/notebook/timg/page_13.bclim"
    "${CMAKE_BINARY_DIR}/notebook/timg/page_14.bclim"
)

# selwin
extract_darc(
    NAME Selwin
    FILE "${DECOMPILER_ROM_DIR}/data/gkk/lyt/selwin.arc"
    OUTPUT "${CMAKE_BINARY_DIR}/selwin"
    DEPENDS Extract3DSROM
)
export_image_bclim(
    NAME Selwin
    OUTPUT "${DECOMPILER_MOD_IMGS_DIR}/selwin"
    DEPENDS ExtractDarcSelwin
    BCLIM_FILES
    "${CMAKE_BINARY_DIR}/selwin/timg/butji_n.bclim"
    "${CMAKE_BINARY_DIR}/selwin/timg/butji_y.bclim"
)

# subscreen
extract_ofs3(
    NAME Subscreen
    FILE "${DECOMPILER_ROM_DIR}/data/gkk/lyt/sub_screen.bin"
    OUTPUT "${CMAKE_BINARY_DIR}/sub_screen"
    DEPENDS Extract3DSROM
)
extract_darc(
    NAME Subscreen
    FILE "${CMAKE_BINARY_DIR}/sub_screen/File0.bin"
    OUTPUT "${CMAKE_BINARY_DIR}/sub_screen0"
    DEPENDS ExtractOfs3Subscreen
)
export_image_bclim(
    NAME Subscreen
    OUTPUT "${DECOMPILER_MOD_IMGS_DIR}/sub_screen"
    DEPENDS ExtractDarcSubscreen
    BCLIM_FILES
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/A_moji.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/auto_moji.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/butji_mdr.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/butji_n.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/butji_y.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/butt_gattai.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/butt_jk.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/epi_tag_0.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/epi_tag_dw0_e.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/epi_tag_dw0_ge.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/epi_tag_g0.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/head_card.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/head_epi.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/head_map00.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/head_map01.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/head_map02.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/head_map03.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/head_map04.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/head_piece.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/head_tool.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/hint_fuki.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/je_head_1.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/je_head_2.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/je_head_3.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/je_head_4.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/kouko_F.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/kouko_moji.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/mai_1.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/mai_2.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/mai_3.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/mai_4.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/mai_5.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/map_01.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/map_02.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/map_04.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/map_mk.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_00_A.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_00_B.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_01_A.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_01_B.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_02_A.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_03_A.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_03_B.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_04_A.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_04_B.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_05_A.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_05_B.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_06_A.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_06_B.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_07_A.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_07_B.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_08_A.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_08_B.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_09_A.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_09_B.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_10_A.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_11_A.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_11_B.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_12_A.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_12_B.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_13_A.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_13_B.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_14_A.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_14_B.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/name_20_A.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/pie_tag_3.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/pie_tag_dw2_e.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/save_moji.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/senko_F.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/senko_moji.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/slct_moji_gt0.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/slct_moji_gt1.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/slct_moji_gt2.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/slct_moji_gt3.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/syobu_fuki.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/tub_card.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/tub_epi.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/tub_piece.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/tub_tool.bclim"
    "${CMAKE_BINARY_DIR}/sub_screen0/timg/zenz_mk_a.bclim"
)

# 5. Extract bclyt texts
export_bclyt_texts(
    TARGET ExportBclytTitle
    OUTPUT "${DECOMPILER_MOD_LAYOUT_DIR}/title"
    DEPENDS ExtractDarcTitle
    BCLYT_FILES
        "${CMAKE_BINARY_DIR}/title/blyt/save_load.bclyt"
        "${CMAKE_BINARY_DIR}/title/blyt/sta_menu.bclyt"
)
export_bclyt_texts(
    TARGET ExportBclytCardlyt
    OUTPUT "${DECOMPILER_MOD_LAYOUT_DIR}/cardlyt"
    DEPENDS ExtractDarcCardlyt
    BCLYT_FILES "${CMAKE_BINARY_DIR}/cardlyt/blyt/kbattle_sita.bclyt"
)
export_bclyt_texts(
    TARGET ExportBclytNotebook
    OUTPUT "${DECOMPILER_MOD_LAYOUT_DIR}/notebook"
    DEPENDS ExtractDarcNotebook
    BCLYT_FILES
        "${CMAKE_BINARY_DIR}/notebook/blyt/techo_sita.bclyt"
        "${CMAKE_BINARY_DIR}/notebook/blyt/techo_ue.bclyt"
)
export_bclyt_texts(
    TARGET ExportBclytSubscreen
    OUTPUT "${DECOMPILER_MOD_LAYOUT_DIR}/subscreen"
    DEPENDS ExtractDarcSubscreen
    BCLYT_FILES
        "${CMAKE_BINARY_DIR}/sub_screen0/blyt/auto_save.bclyt"
        "${CMAKE_BINARY_DIR}/sub_screen0/blyt/epi_titile.bclyt"
        "${CMAKE_BINARY_DIR}/sub_screen0/blyt/msgWin_and_cardBattle.bclyt"
        "${CMAKE_BINARY_DIR}/sub_screen0/blyt/piece_get.bclyt"
        "${CMAKE_BINARY_DIR}/sub_screen0/blyt/sub_card_sita.bclyt"
        "${CMAKE_BINARY_DIR}/sub_screen0/blyt/sub_card_ue.bclyt"
        "${CMAKE_BINARY_DIR}/sub_screen0/blyt/sub_epi.bclyt"
        "${CMAKE_BINARY_DIR}/sub_screen0/blyt/sub_piece.bclyt"
        "${CMAKE_BINARY_DIR}/sub_screen0/blyt/sub_tool.bclyt"
        "${CMAKE_BINARY_DIR}/sub_screen0/blyt/tool_save.bclyt"
)

# 6. Extract 3D textures
# TODO: cardtex: result_make, result_kachi, result_aiko
