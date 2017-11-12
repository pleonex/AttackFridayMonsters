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
function(extract_darc)
    set(options "")
    set(oneValueArgs FILE NAME)
    set(multiValueArgs "")
    cmake_parse_arguments(AFM "${options}" "${oneValueArgs}" "${multiValueArgs}" ${ARGN})

    # Get default name
    if(NOT AFM_NAME)
        get_filename_component(AFM_NAME "${AFM_FILE}" NAME_WE)
    endif()

    # Find tools
    find_program(MONO mono)
    find_program(AFM_TOOL AttackFridayMonsters.exe)
    if(NOT AFM_TOOL)
        message(FATAL_ERROR "Missing generic tool for AttackFridayMonsters formats")
    endif()
    if(NOT WIN32 AND NOT MONO)
        message(FATAL_ERROR "Missing mono installation")
    endif()

    add_custom_command(
        OUTPUT
        "${CMAKE_BINARY_DIR}/${AFM_NAME}/touch.cmake"
        COMMAND
        ${MONO} ${AFM_TOOL} -e darc ${AFM_FILE} ${CMAKE_BINARY_DIR}/${AFM_NAME}
        COMMAND
        ${CMAKE_COMMAND} -E touch ${CMAKE_BINARY_DIR}/${AFM_NAME}/touch.cmake
        COMMENT
        "Extracting DARC ${AFM_NAME}"
        DEPENDS
        Extract3DSROM
    )
    add_custom_target(ExtractDarc${AFM_NAME} ALL
        DEPENDS
        "${CMAKE_BINARY_DIR}/${AFM_NAME}/touch.cmake"
    )
endfunction()
