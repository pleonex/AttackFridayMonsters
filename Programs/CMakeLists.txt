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
project(AttackFridayMonstersTools VERSION 1.0 LANGUAGES C)

set(CMAKE_MODULE_PATH "${PROJECT_SOURCE_DIR}/../CMakeModules")
include(ExternalProject)
include(CSharpProject)

# 1. 3dstool: create ROMs
ExternalProject_add(3dstools SOURCE_DIR "${PROJECT_SOURCE_DIR}/ROMTool")
install(DIRECTORY ROMTool/bin/ DESTINATION tools PATTERN "Debug" EXCLUDE)

# 2. bclimtool: export/import images
ExternalProject_add(bclimtool SOURCE_DIR "${PROJECT_SOURCE_DIR}/bclimtool")
install(DIRECTORY bclimtool/bin/ DESTINATION tools PATTERN "Debug" EXCLUDE)

# 3. Font converter
install(PROGRAMS FontTool/bcfnt.py DESTINATION tools)

# 4. AttackFridayMonsters
# If we depend on the projects too, it will trigger when adding a new file.
add_csharp_target(
    PROJECT "${PROJECT_SOURCE_DIR}/AttackFridayMonsters/AttackFridayMonsters.sln"
    OUTPUT AttackFridayMonsters.exe
    DEPENDS "${PROJECT_SOURCE_DIR}/Yarhl"
    DESTINATION tools
)

# 5. CUE tool
add_executable(lzx "${PROJECT_SOURCE_DIR}/CUETools/lzx.c")
install(TARGETS lzx DESTINATION tools)

# 6. txobtool: export/import textures
ExternalProject_add(txobtool SOURCE_DIR "${PROJECT_SOURCE_DIR}/TextureTool")
install(DIRECTORY TextureTool/bin/ DESTINATION tools PATTERN "Debug" EXCLUDE)
