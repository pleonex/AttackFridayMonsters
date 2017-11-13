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
project(AttackFridayMonstersCompiler VERSION 1.0 LANGUAGES NONE)

# Options
set(COMPILER_OUTPUT_FILE "${PROJECT_SOURCE_DIR}/../GameData/patched.3ds" CACHE STRING "Output patched ROM file")
set(COMPILER_TOOLS_DIR "${PROJECT_SOURCE_DIR}/../GameData/tools" CACHE STRING "Compilation tools")
set(COMPILER_PREFIX "${PROJECT_SOURCE_DIR}/../Decompiler" CACHE STRING "Root directory for compilation")
set(COMPILER_ROM_DIR "${COMPILER_PREFIX}/rom" CACHE STRING "Directory to read ROM files")
set(COMPILER_MOD_DIR "${COMPILER_PREFIX}/mod" CACHE STRING "Directory to read modification files")
set(COMPILER_ROM_INTERNAL_DIR "${COMPILER_ROM_DIR}/internal" CACHE STRING "Directory to read ROM internal data")

# Validation checks
if(NOT EXISTS "${COMPILER_TOOLS_DIR}")
    message(FATAL_ERROR "Missing tool binaries: ${COMPILER_TOOLS_DIR}")
endif()

# Summary
message(STATUS "## Project summary          ##")
message(STATUS "Output file .................: ${COMPILER_OUTPUT_FILE}")
message(STATUS "Tools dir ...................: ${COMPILER_TOOLS_DIR}")
message(STATUS "Prefix path .................: ${COMPILER_PREFIX}")
message(STATUS "ROM dir .....................: ${COMPILER_ROM_DIR}")
message(STATUS "Modifications dir............: ${COMPILER_MOD_DIR}")
message(STATUS "ROM internals dir............: ${COMPILER_ROM_INTERNAL_DIR}")
message(STATUS "##############################")

set(COMPILER_MOD_FONTS_DIR "${COMPILER_MOD_DIR}/Fonts")
set(COMPILER_MOD_TEXTS_DIR "${COMPILER_MOD_DIR}/Texts")
set(COMPILER_MOD_SCRIPTS_DIR "${COMPILER_MOD_TEXTS_DIR}/Scripts")
set(COMPILER_MOD_IMGS_DIR "${COMPILER_MOD_DIR}/Images")

# Find dependencies
list(APPEND CMAKE_MODULE_PATH "${PROJECT_SOURCE_DIR}/../CMakeModules")
set(CMAKE_PROGRAM_PATH "${COMPILER_TOOLS_DIR}")
include(3dsRoms)
include(3dsFonts)
include(3dsImages)
include(AttackFridayMonsters)

# 1. Convert font
extract_darc(
    NAME Title
    FILE "${COMPILER_ROM_DIR}/data/gkk/lyt/title.arc"
    OUTPUT "${CMAKE_BINARY_DIR}/title"
)
import_3ds_font(
    NAME Afm
    FONT_DIR "${COMPILER_MOD_FONTS_DIR}"
    OUTPUT "${CMAKE_BINARY_DIR}/title/font/kk_KN_Font.bcfnt"
    DEPENDS ExtractDarcTitle
)
# We will pack after importing title images

# 2. Convert texts
# 2.1. Card related texts


# 2.2 Episode titles


# 2.3 Binary text
# TODO

# 2.4 Scripts


# 3. Images
# title
# Already exported for the font
import_image_bclim(
    NAME Title
    OUTPUT "${CMAKE_BINARY_DIR}/title/timg"
    DEPENDS ExtractDarcTitle
    PNG_FILES
    "${COMPILER_MOD_IMGS_DIR}/title/auto_mk.png"
    "${COMPILER_MOD_IMGS_DIR}/title/butji_mdr.png"
    "${COMPILER_MOD_IMGS_DIR}/title/butji_n.png"
    "${COMPILER_MOD_IMGS_DIR}/title/butji_y.png"
    "${COMPILER_MOD_IMGS_DIR}/title/epi_moji.png"
    "${COMPILER_MOD_IMGS_DIR}/title/head_load.png"
    "${COMPILER_MOD_IMGS_DIR}/title/head_save.png"
    "${COMPILER_MOD_IMGS_DIR}/title/title_rogo.png"

)
import_darc(
    NAME Title
    INPUT "${CMAKE_BINARY_DIR}/title"
    OUTPUT "${COMPILER_ROM_DIR}/data/gkk/lyt/title.arc"
    DEPENDS ImportFontAfm ImportBclimTitle
)

# bumper
extract_darc(
    NAME Bumper
    FILE "${COMPILER_ROM_DIR}/data/gkk/lyt/bumper.arc"
    OUTPUT "${CMAKE_BINARY_DIR}/bumper"
)
import_image_bclim(
    NAME Bumper
    OUTPUT "${CMAKE_BINARY_DIR}/bumper/timg"
    DEPENDS ExtractDarcBumper
    PNG_FILES
    "${COMPILER_MOD_IMGS_DIR}/bumper/ex_msg_A.png"
    "${COMPILER_MOD_IMGS_DIR}/bumper/ex_msg_B.png"
    "${COMPILER_MOD_IMGS_DIR}/bumper/ex_msg_C.png"
    "${COMPILER_MOD_IMGS_DIR}/bumper/stereo_ji.png"

)
import_darc(
    NAME Bumper
    INPUT "${CMAKE_BINARY_DIR}/bumper"
    OUTPUT "${COMPILER_ROM_DIR}/data/gkk/lyt/bumper.arc"
    DEPENDS ImportBclimBumper
)

# cardly_d
extract_darc(
    NAME Cardlyt
    FILE "${COMPILER_ROM_DIR}/data/gkk/cardgame/cardlyt_d.arc"
    OUTPUT "${CMAKE_BINARY_DIR}/cardlyt"
)
import_image_bclim(
    NAME Cardlyt
    OUTPUT "${CMAKE_BINARY_DIR}/cardlyt/timg"
    DEPENDS ExtractDarcCardlyt
    PNG_FILES
    "${COMPILER_MOD_IMGS_DIR}/cardlyt/butji_mdr.png"
    "${COMPILER_MOD_IMGS_DIR}/cardlyt/butji_n.png"
    "${COMPILER_MOD_IMGS_DIR}/cardlyt/butji_y.png"
    "${COMPILER_MOD_IMGS_DIR}/cardlyt/chan_moji.png"
    "${COMPILER_MOD_IMGS_DIR}/cardlyt/dasu_butt.png"
    "${COMPILER_MOD_IMGS_DIR}/cardlyt/mai_1.png"
    "${COMPILER_MOD_IMGS_DIR}/cardlyt/mai_2.png"
    "${COMPILER_MOD_IMGS_DIR}/cardlyt/mai_3.png"
    "${COMPILER_MOD_IMGS_DIR}/cardlyt/mai_4.png"
    "${COMPILER_MOD_IMGS_DIR}/cardlyt/mai_5.png"
)
import_darc(
    NAME Cardlyt
    INPUT "${CMAKE_BINARY_DIR}/cardlyt"
    OUTPUT "${COMPILER_ROM_DIR}/data/gkk/cardgame/cardlyt_d.arc"
    DEPENDS ImportBclimCardlyt
)

# movie_low
extract_darc(
    NAME Movielow
    FILE "${COMPILER_ROM_DIR}/data/gkk/lyt/movie_low.arc"
    OUTPUT "${CMAKE_BINARY_DIR}/movie_low"
)
import_image_bclim(
    NAME Movielow
    OUTPUT "${CMAKE_BINARY_DIR}/movie_low/timg"
    DEPENDS ExtractDarcMovielow
    PNG_FILES
    "${COMPILER_MOD_IMGS_DIR}/movie_low/skip_ji.png"
)
import_darc(
    NAME Movielow
    INPUT "${CMAKE_BINARY_DIR}/movie_low"
    OUTPUT "${COMPILER_ROM_DIR}/data/gkk/lyt/movie_low.arc"
    DEPENDS ImportBclimMovielow
)

# notebook
extract_darc(
    NAME Notebook
    FILE "${COMPILER_ROM_DIR}/data/gkk/lyt/notebook.arc"
    OUTPUT "${CMAKE_BINARY_DIR}/notebook"
)
import_image_bclim(
    NAME Notebook
    OUTPUT "${CMAKE_BINARY_DIR}/notebook/timg"
    DEPENDS ExtractDarcNotebook
    PNG_FILES
    "${COMPILER_MOD_IMGS_DIR}/notebook/auto_moji.png"
    "${COMPILER_MOD_IMGS_DIR}/notebook/butji_mdr.png"
    "${COMPILER_MOD_IMGS_DIR}/notebook/page_00.png"
    "${COMPILER_MOD_IMGS_DIR}/notebook/page_01.png"
    "${COMPILER_MOD_IMGS_DIR}/notebook/page_02a.png"
    "${COMPILER_MOD_IMGS_DIR}/notebook/page_02b.png"
    "${COMPILER_MOD_IMGS_DIR}/notebook/page_03.png"
    "${COMPILER_MOD_IMGS_DIR}/notebook/page_04.png"
    "${COMPILER_MOD_IMGS_DIR}/notebook/page_05a.png"
    "${COMPILER_MOD_IMGS_DIR}/notebook/page_05b.png"
    "${COMPILER_MOD_IMGS_DIR}/notebook/page_06a.png"
    "${COMPILER_MOD_IMGS_DIR}/notebook/page_06b.png"
    "${COMPILER_MOD_IMGS_DIR}/notebook/page_07.png"
    "${COMPILER_MOD_IMGS_DIR}/notebook/page_08.png"
    "${COMPILER_MOD_IMGS_DIR}/notebook/page_09.png"
    "${COMPILER_MOD_IMGS_DIR}/notebook/page_10.png"
    "${COMPILER_MOD_IMGS_DIR}/notebook/page_11.png"
    "${COMPILER_MOD_IMGS_DIR}/notebook/page_12.png"
    "${COMPILER_MOD_IMGS_DIR}/notebook/page_13.png"
    "${COMPILER_MOD_IMGS_DIR}/notebook/page_14.png"
)
import_darc(
    NAME Notebook
    INPUT "${CMAKE_BINARY_DIR}/notebook"
    OUTPUT "${COMPILER_ROM_DIR}/data/gkk/lyt/notebook.arc"
    DEPENDS ImportBclimNotebook
)

# selwin
extract_darc(
    NAME Selwin
    FILE "${COMPILER_ROM_DIR}/data/gkk/lyt/selwin.arc"
    OUTPUT "${CMAKE_BINARY_DIR}/selwin"
)
import_image_bclim(
    NAME Selwin
    OUTPUT "${CMAKE_BINARY_DIR}/selwin/timg"
    DEPENDS ExtractDarcSelwin
    PNG_FILES
    "${COMPILER_MOD_IMGS_DIR}/selwin/butji_n.png"
    "${COMPILER_MOD_IMGS_DIR}/selwin/butji_y.png"
)
import_darc(
    NAME Selwin
    INPUT "${CMAKE_BINARY_DIR}/selwin"
    OUTPUT "${COMPILER_ROM_DIR}/data/gkk/lyt/selwin.arc"
    DEPENDS ImportBclimSelwin
)

# subscreen


# 4. Create ROM
pack_3ds_rom(
    ROM_DIR "${COMPILER_ROM_DIR}"
    INTERNAL_DIR "${COMPILER_ROM_INTERNAL_DIR}"
    OUTPUT "${COMPILER_OUTPUT_FILE}"
    DEPENDS
    PackDarcTitle PackDarcBumper PackDarcCardlyt PackDarcMovielow PackDarcNotebook PackDarcSelwin
)
