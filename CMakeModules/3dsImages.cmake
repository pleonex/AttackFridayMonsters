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
function(export_image_bclim)
    set(options "")
    set(oneValueArgs OUTPUT NAME)
    set(multiValueArgs BCLIM_FILES DEPENDS)
    cmake_parse_arguments(3DS_IMG "${options}" "${oneValueArgs}" "${multiValueArgs}" ${ARGN})

    # Find tools
    find_program(3DS_IMG_TOOL bclimtool)
    if(NOT 3DS_IMG_TOOL)
        message(FATAL_ERROR "Missing bclimtool for images")
    endif()
    get_filename_component(3DS_IMG_TOOLS_DIR "${3DS_IMG_TOOL}" DIRECTORY)

    file(MAKE_DIRECTORY "${3DS_IMG_OUTPUT}")
    foreach(3DS_IMG_BCLIM ${3DS_IMG_BCLIM_FILES})
        # Get output png file and append to images list
        get_filename_component(3DS_IMG_BCLIM_NAME "${3DS_IMG_BCLIM}" NAME_WE)
        set(3DS_IMG_OUTPUT_IMAGE "${3DS_IMG_OUTPUT}/${3DS_IMG_BCLIM_NAME}.png")
        list(APPEND 3DS_IMG_OUTPUT_IMAGES "${3DS_IMG_OUTPUT_IMAGE}")

        add_custom_command(
            OUTPUT
            "${3DS_IMG_OUTPUT_IMAGE}"
            COMMAND
            ${3DS_IMG_TOOL} -dfp ${3DS_IMG_BCLIM} ${3DS_IMG_OUTPUT_IMAGE}
            COMMENT
            "Exporting BCLIM image ${3DS_IMG_NAME}/${3DS_IMG_BCLIM_NAME}"
            WORKING_DIRECTORY
            "${3DS_IMG_TOOLS_DIR}"
            DEPENDS
            ${3DS_IMG_DEPENDS}
        )
    endforeach()

    # Link all the images to single target
    add_custom_target(ExtractBclim${3DS_IMG_NAME} ALL DEPENDS ${3DS_IMG_OUTPUT_IMAGES})
endfunction()

function(import_image_bclim)
    set(options "")
    set(oneValueArgs OUTPUT NAME)
    set(multiValueArgs PNG_FILES DEPENDS)
    cmake_parse_arguments(3DS_IMG "${options}" "${oneValueArgs}" "${multiValueArgs}" ${ARGN})

    # Find tools
    find_program(3DS_IMG_TOOL bclimtool)
    if(NOT 3DS_IMG_TOOL)
        message(FATAL_ERROR "Missing bclimtool for images")
    endif()
    get_filename_component(3DS_IMG_TOOLS_DIR "${3DS_IMG_TOOL}" DIRECTORY)

    foreach(3DS_IMG_PNG ${3DS_IMG_PNG_FILES})
        # Get output bclim file and append to images list
        get_filename_component(3DS_IMG_PNG_NAME "${3DS_IMG_PNG}" NAME_WE)
        set(3DS_IMG_OUTPUT_BCLIM "${3DS_IMG_OUTPUT}/${3DS_IMG_PNG_NAME}.bclim")
        list(APPEND 3DS_IMG_OUTPUT_IMAGES "${3DS_IMG_OUTPUT_BCLIM}_fake")

        add_custom_command(
            OUTPUT
            "${3DS_IMG_OUTPUT_BCLIM}_fake"
            COMMAND
            ${3DS_IMG_TOOL} -efp ${3DS_IMG_OUTPUT_BCLIM} ${3DS_IMG_PNG}
            COMMENT
            "Importing BCLIM image ${3DS_IMG_NAME}/${3DS_IMG_PNG_NAME}"
            WORKING_DIRECTORY
            "${3DS_IMG_TOOLS_DIR}"
            DEPENDS
            ${3DS_IMG_DEPENDS}
        )
    endforeach()

    # Link all the images to single target
    add_custom_target(ImportBclim${3DS_IMG_NAME} ALL DEPENDS ${3DS_IMG_OUTPUT_IMAGES})
endfunction()

function(export_image_cgfx)
    set(options NO_EXTRA_DIR)
    set(oneValueArgs OUTPUT TARGET)
    set(multiValueArgs CGFX_FILES DEPENDS)
    cmake_parse_arguments(3DS_IMG "${options}" "${oneValueArgs}" "${multiValueArgs}" ${ARGN})

    # Find tools
    find_program(3DS_IMG_TOOL2 txobtool)
    if(NOT 3DS_IMG_TOOL2)
        message(FATAL_ERROR "Missing txobtool for images")
    endif()
    get_filename_component(3DS_IMG_TOOLS_DIR "${3DS_IMG_TOOL2}" DIRECTORY)

    file(MAKE_DIRECTORY "${3DS_IMG_OUTPUT}")
    foreach(3DS_IMG_CGFX ${3DS_IMG_CGFX_FILES})
        # Get output png file and append to images list
        get_filename_component(3DS_IMG_CGFX_NAME "${3DS_IMG_CGFX}" NAME_WE)
        if(3DS_IMG_NO_EXTRA_DIR)
            set(3DS_IMG_OUTPUT_DIR "${3DS_IMG_OUTPUT}")
        else()
            set(3DS_IMG_OUTPUT_DIR "${3DS_IMG_OUTPUT}/${3DS_IMG_CGFX_NAME}")
        endif()
        list(APPEND 3DS_IMG_OUTPUT_IMAGES "${3DS_IMG_OUTPUT_DIR}/${3DS_IMG_CGFX_NAME}")

        add_custom_command(
            OUTPUT "${3DS_IMG_OUTPUT_DIR}/${3DS_IMG_CGFX_NAME}"
            COMMAND ${3DS_IMG_TOOL2} -efd ${3DS_IMG_CGFX} ${3DS_IMG_OUTPUT_DIR}
            COMMENT "Exporting CGFX images for ${3DS_IMG_TARGET}: ${3DS_IMG_CGFX_NAME}"
            WORKING_DIRECTORY "${3DS_IMG_TOOLS_DIR}"
            DEPENDS ${3DS_IMG_DEPENDS}
        )
    endforeach()

    # Link all the images to single target
    add_custom_target(${3DS_IMG_TARGET} ALL DEPENDS ${3DS_IMG_OUTPUT_IMAGES})
endfunction()
