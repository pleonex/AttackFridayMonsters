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
cmake_minimum_required(VERSION 3.17)
project(AttackFridayMonstersTools VERSION 1.0 LANGUAGES C)

set(CMAKE_MODULE_PATH "${PROJECT_SOURCE_DIR}/CMakeModules")
include(ExternalProject)
include(CheckTypeSize)
include(CSharpProject)

# 1. 3dstool: create ROMs
ExternalProject_add(3dstools
    SOURCE_DIR "${PROJECT_SOURCE_DIR}/ROMTool"
    CMAKE_CACHE_ARGS
        -DCMAKE_BUILD_TYPE:STRING=${CMAKE_BUILD_TYPE}
)
install(
    DIRECTORY "${PROJECT_SOURCE_DIR}/ROMTool/bin/"
    DESTINATION tools
    FILE_PERMISSIONS OWNER_EXECUTE OWNER_WRITE OWNER_READ GROUP_EXECUTE GROUP_READ
    PATTERN ${CMAKE_BUILD_TYPE} EXCLUDE
)

# 2. bclimtool: export/import images
ExternalProject_add(bclimtool
    SOURCE_DIR "${PROJECT_SOURCE_DIR}/bclimtool"
    CMAKE_CACHE_ARGS
        -DCMAKE_BUILD_TYPE:STRING=${CMAKE_BUILD_TYPE}
)
install(
    DIRECTORY "${PROJECT_SOURCE_DIR}/bclimtool/bin/"
    DESTINATION tools
    FILE_PERMISSIONS OWNER_EXECUTE OWNER_WRITE OWNER_READ GROUP_EXECUTE GROUP_READ
    PATTERN ${CMAKE_BUILD_TYPE} EXCLUDE
)

# 3. Font converter
install(PROGRAMS FontTool/bcfnt.py DESTINATION tools)

# 4. AttackFridayMonsters
# If we depend on the projects too, it will trigger when adding a new file.
add_csharp_target(
    PROJECT "${PROJECT_SOURCE_DIR}/AttackFridayMonsters/AttackFridayMonsters.Formats/AttackFridayMonsters.Formats.csproj"
    OUTPUT AttackFridayMonsters.Formats.dll
    DESTINATION tools
)

# 5. Lemon
add_csharp_target(
    PROJECT "${PROJECT_SOURCE_DIR}/Lemon/src/Lemon/Lemon.csproj"
    OUTPUT Lemon.dll
    DESTINATION tools
)

# 6. CUE tool
add_executable(lzx "${PROJECT_SOURCE_DIR}/CUETools/lzx.c")
install(TARGETS lzx DESTINATION tools)

add_executable(blz "${PROJECT_SOURCE_DIR}/CUETools/blz.c")
install(TARGETS blz DESTINATION tools)

# 7. txobtool: export/import textures
ExternalProject_add(txobtool
    SOURCE_DIR "${PROJECT_SOURCE_DIR}/TextureTool"
    CMAKE_CACHE_ARGS
        -DCMAKE_BUILD_TYPE:STRING=${CMAKE_BUILD_TYPE}
)
install(
    DIRECTORY "${PROJECT_SOURCE_DIR}/TextureTool/bin/"
    DESTINATION tools
    FILE_PERMISSIONS OWNER_EXECUTE OWNER_WRITE OWNER_READ GROUP_EXECUTE GROUP_READ
    PATTERN ${CMAKE_BUILD_TYPE} EXCLUDE
)

# 8. code.bin helper file
install(FILES "${PROJECT_SOURCE_DIR}/code_text.yml" DESTINATION tools)

# 9. XDelta3
add_subdirectory(xdelta)
