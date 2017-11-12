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
function(export_3ds_font)
    set(options "")
    set(oneValueArgs FONT OUTPUT)
    set(multiValueArgs DEPENDS)
    cmake_parse_arguments(EXTRACT_3DS "${options}" "${oneValueArgs}" "${multiValueArgs}" ${ARGN})

    # Get and check dependencies
    find_package(PythonInterp REQUIRED)
    find_program(EXTRACT_3DS_FONT_TOOL bcfnt.py)
    if(NOT EXTRACT_3DS_FONT_TOOL)
        message(FATAL_ERROR "Missing font tool")
    endif()

    # Target rule
    get_filename_component(EXTRACT_3DS_FONT_NAME ${EXTRACT_3DS_FONT} NAME_WE)
    add_custom_target(ExportFont${EXTRACT_3DS_FONT_NAME} ALL
        COMMAND
        ${PYTHON_EXECUTABLE} ${EXTRACT_3DS_FONT_TOOL} -x -y -f ${EXTRACT_3DS_FONT}
        COMMENT
        "Exporting font ${EXTRACT_3DS_FONT_NAME}"
        WORKING_DIRECTORY
        ${EXTRACT_3DS_OUTPUT}
        DEPENDS
        ${EXTRACT_3DS_DEPENDS}
    )
endfunction()
