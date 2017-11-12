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
cmake_minimum_required(VERSION 3.9)
project(AttackFridayMonsters VERSION 1.0 LANGUAGES NONE)

include(ExternalProject)

set(ACTION "decompile" CACHE STRING "Compile/Decompile action")
string(TOLOWER ACTION ${ACTION})

# Compile tools
ExternalProject_add(Tools
    SOURCE_DIR "${PROJECT_SOURCE_DIR}/Programs"
    CMAKE_ARGS "-DCMAKE_INSTALL_PREFIX=${PROJECT_SOURCE_DIR}/GameData")

if(ACTION STREQUAL "decompile")
    ExternalProject_add(Decompile
        SOURCE_DIR "${PROJECT_SOURCE_DIR}/Decompiler"
        DEPENDS Tools
        INSTALL_COMMAND "")
endif()
