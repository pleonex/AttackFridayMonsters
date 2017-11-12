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
function(extract_3ds_rom)
    set(options "")
    set(oneValueArgs ROM ROM_DIR INTERNAL_DIR)
    set(multiValueArgs "")
    cmake_parse_arguments(EXTRACT_3DS "${options}" "${oneValueArgs}" "${multiValueArgs}" ${ARGN})

    # Find the tool
    find_program(EXTRACT_3DS_ROM_TOOL 3dstool)
    if(NOT EXTRACT_3DS_ROM_TOOL)
        message(FATAL_ERROR "Missing 3dstool for ROMs")
    endif()

    add_custom_command(
        OUTPUT
        "${CMAKE_BINARY_DIR}/exefs.bin"
        "${CMAKE_BINARY_DIR}/romfs.bin"

        # Extract the game partition from the .3ds / cci file
        COMMAND
        ${EXTRACT_3DS_ROM_TOOL} -xt0f cci
            ${CMAKE_BINARY_DIR}/game.bin
            ${EXTRACT_3DS_ROM}
            --header ${EXTRACT_3DS_INTERNAL_DIR}/header_cci.bin

        # Extract the file systems from the game
        COMMAND
        ${EXTRACT_3DS_ROM_TOOL} -xtf cxi ${CMAKE_BINARY_DIR}/game.bin
            --exefs ${CMAKE_BINARY_DIR}/exefs.bin
            --romfs ${CMAKE_BINARY_DIR}/romfs.bin
            --header ${EXTRACT_3DS_INTERNAL_DIR}/header_ncch0.bin
            --exh ${EXTRACT_3DS_INTERNAL_DIR}/exheader_ncch0.bin
            --plain ${EXTRACT_3DS_INTERNAL_DIR}/plain.bin

        # Extract the system files
        COMMAND
        ${EXTRACT_3DS_ROM_TOOL} -xtfu exefs ${CMAKE_BINARY_DIR}/exefs.bin
            --exefs-dir ${EXTRACT_3DS_ROM_DIR}/system
            --header ${EXTRACT_3DS_INTERNAL_DIR}/header_system.bin

        # Extract the ROM files
        COMMAND
        ${EXTRACT_3DS_ROM_TOOL} -xtf romfs ${CMAKE_BINARY_DIR}/romfs.bin
            --romfs-dir ${EXTRACT_3DS_ROM_DIR}/data

        DEPENDS
        "${EXTRACT_3DS_ROM}"
        COMMENT
        "Extracting ROM ${EXTRACT_3DS_ROM}"
    )

    add_custom_target(Extract3DSROM ALL
        DEPENDS
        "${CMAKE_BINARY_DIR}/exefs.bin"
        "${CMAKE_BINARY_DIR}/romfs.bin"
    )
endfunction()
