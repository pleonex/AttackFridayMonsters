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
project(AttackFridayMonstersTools VERSION 1.0)

include(ExternalProject)

# 1. 3dstool: create ROMs
ExternalProject_add(3dstools SOURCE_DIR "${PROJECT_SOURCE_DIR}/ROMTool")
install(PROGRAMS ROMTool/bin/3dstool DESTINATION tools)
install(FILES ROMTool/bin/ignore_3dstool.txt DESTINATION tools)

# 2. bclimtool: export/import images
ExternalProject_add(bclimtool SOURCE_DIR "${PROJECT_SOURCE_DIR}/bclimtool")
install(PROGRAMS bclimtool/bin/bclimtool DESTINATION tools)
install(FILES bclimtool/bin/libPVRTexLib.so DESTINATION tools)

# 3. Font converter
install(PROGRAMS FontTool/bcfnt.py DESTINATION tools)

# 4. AttackFridayMonsters
add_custom_target(AttackFridayMonsters ALL
    COMMAND msbuild /v:minimal /p:OutputPath=\"${CMAKE_BINARY_DIR}\"/AttackFridayMonsters \"${PROJECT_SOURCE_DIR}\"/AttackFridayMonsters/AttackFridayMonsters.sln)
install(DIRECTORY "${CMAKE_BINARY_DIR}/AttackFridayMonsters/"
    DESTINATION tools
    USE_SOURCE_PERMISSIONS
    FILES_MATCHING PATTERN "*" PATTERN "*.pdb" EXCLUDE)

# 5. CUE tool
add_executable(lzx "${PROJECT_SOURCE_DIR}/CUETools/lzx.c")
install(TARGETS lzx DESTINATION tools)
