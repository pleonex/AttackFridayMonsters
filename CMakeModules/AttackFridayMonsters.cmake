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
macro(find_afm_tool)
    find_program(MONO mono)
    find_program(AFM_TOOL AttackFridayMonsters.exe)
    if(NOT AFM_TOOL)
        message(FATAL_ERROR "Missing generic tool for AttackFridayMonsters formats")
    endif()
    if(NOT WIN32 AND NOT MONO)
        message(FATAL_ERROR "Missing mono installation")
    endif()
endmacro()

function(extract_darc)
    set(options "")
    set(oneValueArgs FILE NAME OUTPUT)
    set(multiValueArgs "")
    cmake_parse_arguments(AFM "${options}" "${oneValueArgs}" "${multiValueArgs}" ${ARGN})

    # Get default name
    if(NOT AFM_NAME)
        get_filename_component(AFM_NAME "${AFM_FILE}" NAME_WE)
    endif()

    find_afm_tool()
    add_custom_command(
        OUTPUT
        "${AFM_OUTPUT}/touch.cmake"
        COMMAND
        ${MONO} ${AFM_TOOL} -e darc ${AFM_FILE} ${AFM_OUTPUT}
        COMMAND
        ${CMAKE_COMMAND} -E touch ${AFM_OUTPUT}/touch.cmake
        COMMENT
        "Extracting DARC ${AFM_NAME}"
        DEPENDS
        Extract3DSROM
    )
    add_custom_target(ExtractDarc${AFM_NAME} ALL
        DEPENDS
        "${AFM_OUTPUT}/touch.cmake"
    )
endfunction()

function(export_card_texts)
    set(options "")
    set(oneValueArgs FILE OUTPUT)
    set(multiValueArgs "")
    cmake_parse_arguments(AFM "${options}" "${oneValueArgs}" "${multiValueArgs}" ${ARGN})

    find_afm_tool()
    add_custom_command(
        OUTPUT
        "${AFM_OUTPUT}/cardinfo.po"
        "${AFM_OUTPUT}/cardgame_dialogs.po"
        COMMAND
        ${MONO} ${AFM_TOOL} -e carddata0 ${AFM_FILE} ${AFM_OUTPUT}/cardinfo.po
        COMMAND
        ${MONO} ${AFM_TOOL} -e carddata25 ${AFM_FILE} ${AFM_OUTPUT}/cardgame_dialogs.po
        COMMENT
        "Exporting cardgame texts"
        DEPENDS
        Extract3DSROM
    )
    add_custom_target(AfmCardText ALL
        DEPENDS
        "${AFM_OUTPUT}/cardinfo.po"
        "${AFM_OUTPUT}/cardgame_dialogs.po"
    )
endfunction()

function(export_episodes_titles)
    set(options "")
    set(oneValueArgs FILE OUTPUT)
    set(multiValueArgs "")
    cmake_parse_arguments(AFM "${options}" "${oneValueArgs}" "${multiValueArgs}" ${ARGN})

    find_afm_tool()
    add_custom_command(
        OUTPUT
        "${AFM_OUTPUT}/episodes_title.po"
        COMMAND
        ${MONO} ${AFM_TOOL} -e episode ${AFM_FILE} ${AFM_OUTPUT}/episodes_title.po
        COMMENT
        "Exporting episodes title"
        DEPENDS
        Extract3DSROM
    )
    add_custom_target(AfmEpisodesTitle ALL
        DEPENDS
        "${AFM_OUTPUT}/episodes_title.po"
    )
endfunction()

function(export_scripts_text)
    set(options "")
    set(oneValueArgs OUTPUT)
    set(multiValueArgs MAP_FILES)
    cmake_parse_arguments(AFM "${options}" "${oneValueArgs}" "${multiValueArgs}" ${ARGN})

    # Get tools
    find_afm_tool()
    get_filename_component(AFM_TOOLS_DIR "${AFM_TOOL}" DIRECTORY)

    # For each map
    set(AFM_TEXT_SCRIPTS "")
    foreach(AFM_MAP_FILE ${AFM_MAP_FILES})
        # Get output file and append to full output files list
        get_filename_component(AFM_MAP_NAME "${AFM_MAP_FILE}" NAME_WE)
        set(AFM_TEXT_SCRIPT "${AFM_OUTPUT}/${AFM_MAP_NAME}.po")
        list(APPEND AFM_TEXT_SCRIPTS "${AFM_TEXT_SCRIPT}")

        add_custom_command(
            OUTPUT
            "${AFM_TEXT_SCRIPT}"
            COMMAND
            ${MONO} ${AFM_TOOL} -e script ${AFM_MAP_FILE} ${AFM_TEXT_SCRIPT}
            COMMENT
            "Exporting text script ${AFM_MAP_NAME}"
            DEPENDS
            Extract3DSROM
            WORKING_DIRECTORY
            "${AFM_TOOLS_DIR}"
        )
    endforeach()

    # Link all the script custom commands into a single target
    add_custom_target(ExtractScripts ALL DEPENDS ${AFM_TEXT_SCRIPTS})
endfunction()
