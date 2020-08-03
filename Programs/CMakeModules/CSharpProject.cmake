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
function(add_csharp_target)
    set(options "")
    set(oneValueArgs PROJECT OUTPUT DESTINATION)
    set(multiValueArgs "")
    cmake_parse_arguments(CSHARP_TARGET "${options}" "${oneValueArgs}" "${multiValueArgs}" ${ARGN})

    # Check arguments
    if(NOT CSHARP_TARGET_PROJECT)
        message(FATAL_ERROR "Missing C# project path")
    endif()
    if(NOT CSHARP_TARGET_OUTPUT)
        message(FATAL_ERROR "Missing C# output assembly name")
    endif()

    get_filename_component(CSHARP_TARGET_PROJECT_NAME "${CSHARP_TARGET_PROJECT}" NAME_WE)
    set(CSHARP_TARGET_OUTPUT_DIR "${CMAKE_BINARY_DIR}/${CSHARP_TARGET_PROJECT_NAME}")

    # We use a custom target that runs always (it doesn't have a custom_command)
    # because in the new SDK-style projects there is no way to know if new files
    # has been added.
    add_custom_target(${CSHARP_TARGET_PROJECT_NAME} ALL
        COMMAND dotnet publish -o "${CSHARP_TARGET_OUTPUT_DIR}" "${CSHARP_TARGET_PROJECT}"
        DEPENDS ${CSHARP_TARGET_PROJECT}
    )

    # Finally install (publish)
    if(CSHARP_TARGET_DESTINATION)
        install(DIRECTORY "${CSHARP_TARGET_OUTPUT_DIR}/"
            DESTINATION "${CSHARP_TARGET_DESTINATION}"
            USE_SOURCE_PERMISSIONS
            FILES_MATCHING PATTERN "*"
        )
    endif()
endfunction()
