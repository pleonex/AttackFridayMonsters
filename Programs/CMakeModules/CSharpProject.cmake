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
    set(multiValueArgs DEPENDS)
    cmake_parse_arguments(CSHARP_TARGET "${options}" "${oneValueArgs}" "${multiValueArgs}" ${ARGN})

    # Check arguments
    if(NOT CSHARP_TARGET_PROJECT)
        message(FATAL_ERROR "Missing C# project path")
    endif()
    if(NOT CSHARP_TARGET_OUTPUT)
        message(FATAL_ERROR "Missing C# output assembly name")
    endif()

    get_filename_component(CSHARP_TARGET_PROJECT_NAME "${CSHARP_TARGET_OUTPUT}" NAME_WE)
    get_filename_component(CSHARP_TARGET_PROJECT_DIR "${CSHARP_TARGET_PROJECT}" DIRECTORY)

    # Get all the source files to depend on them and rebuild only if changes
    # We depend on solutions and projects to rebuild on new files / delete files
    # or new projects.
    file(GLOB_RECURSE CSHARP_TARGET_SOURCES
        "${CSHARP_TARGET_PROJECT_DIR}/*.cs"
        "${CSHARP_TARGET_PROJECT_DIR}/*.csproj"
        "${CSHARP_TARGET_PROJECT_DIR}/*.sln"
    )

    # We want to depend on the dependency folders in case they change too.
    foreach(CSHARP_TARGET_DEPENDENCY ${CSHARP_TARGET_DEPENDS})
        file(GLOB_RECURSE CSHARP_TARGET_DEPENDENCY_SOURCES
            "${CSHARP_TARGET_DEPENDENCY}/*.cs"
            "${CSHARP_TARGET_DEPENDENCY}/*.csproj"
            "${CSHARP_TARGET_DEPENDENCY}/*.sln"
        )
        list(APPEND CSHARP_TARGET_SOURCES ${CSHARP_TARGET_DEPENDENCY_SOURCES})
    endforeach()

    # Add a custom command to build the project.
    set(CSHARP_TARGET_OUTPUT_DIR "${CMAKE_BINARY_DIR}/${CSHARP_TARGET_PROJECT_NAME}")
    add_custom_command(
        OUTPUT
        "${CSHARP_TARGET_OUTPUT_DIR}/${CSHARP_TARGET_OUTPUT}"
        COMMAND
        msbuild /v:minimal /m:8
            /p:OutputPath=${CSHARP_TARGET_OUTPUT_DIR}
            ${CSHARP_TARGET_PROJECT}
        DEPENDS
        ${CSHARP_TARGET_SOURCES}
    )

    # Add the target to run always. This target will trigger the custom command
    # and build only if source changes.
    add_custom_target(${CSHARP_TARGET_PROJECT_NAME} ALL
        DEPENDS
        "${CSHARP_TARGET_OUTPUT_DIR}/${CSHARP_TARGET_OUTPUT}"
    )

    # Finally install
    if(CSHARP_TARGET_DESTINATION)
        install(DIRECTORY "${CSHARP_TARGET_OUTPUT_DIR}/"
            DESTINATION "${CSHARP_TARGET_DESTINATION}"
            USE_SOURCE_PERMISSIONS
            FILES_MATCHING PATTERN "*" PATTERN "*.pdb" EXCLUDE
        )
    endif()
endfunction()
