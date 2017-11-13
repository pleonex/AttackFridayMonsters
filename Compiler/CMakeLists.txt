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
# TODO: Pack DARC again

# 2. Convert texts
# 2.1. Card related texts


# 2.2 Episode titles


# 2.3 Binary text
# TODO

# 2.4 Scripts


# 3. Images
# title


# bumper


# cardly_d


# movie_low


# notebook


# selwin


# subscreen


# 4. Create ROM
pack_3ds_rom(
    ROM_DIR "${COMPILER_ROM_DIR}"
    INTERNAL_DIR "${COMPILER_ROM_INTERNAL_DIR}"
    OUTPUT "${COMPILER_OUTPUT_FILE}"
    DEPENDS
    ImportFontAfm
)
