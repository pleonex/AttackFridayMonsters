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
project(AttackFridayMonstersDecompiler VERSION 1.0)

set(OUTPUT_DIR "${PROJECT_SOURCE_DIR}" CACHE STRING "Output directory")
set(ROM_DIR "${OUTPUT_DIR}/rom")
set(MOD_DIR "${OUTPUT_DIR}/mod")
set(INTERNAL_DIR "${OUTPUT_DIR}/internal")
set(ROM_FILE "${PROJECT_SOURCE_DIR}/../GameData/game.3ds" CACHE STRING "Game file")
set(TOOLS_DIR "${PROJECT_SOURCE_DIR}/../GameData/tools" CACHE STRING "Tools")

find_package(PythonInterp REQUIRED)

if(NOT WIN32)
    find_program(MONO mono)
    if(NOT MONO)
        message(FATAL_ERROR "Missing mono installation")
    endif()
endif()

if(NOT EXISTS "${ROM_FILE}")
    message(FATAL_ERROR "Missing input file: ${ROM_FILE}")
endif()

if(NOT EXISTS "${TOOLS_DIR}")
    message(WARNING "Missing tools dir: ${TOOLS_DIR}")
endif()


set(CMAKE_PROGRAM_PATH "${TOOLS_DIR}")
file(MAKE_DIRECTORY ${INTERNAL_DIR})
file(MAKE_DIRECTORY ${ROM_DIR})
file(MAKE_DIRECTORY ${MOD_DIR})

# 1. extract ROM
find_program(ROM_TOOL 3dstool)
add_custom_target(Extract3DS
    COMMAND
    ${ROM_TOOL} -xt01f cci
        ${CMAKE_BINARY_DIR}/game.bin ${INTERNAL_DIR}/manual.bin
        ${ROM_FILE}
        --header "${INTERNAL_DIR}/header_cci.bin"
)

add_custom_target(ExtractGame
    COMMAND
    ${ROM_TOOL} -xtf cxi ${CMAKE_BINARY_DIR}/game.bin
        --exefs ${CMAKE_BINARY_DIR}/exefs.bin
        --romfs ${CMAKE_BINARY_DIR}/romfs.bin
        --header ${INTERNAL_DIR}/header_ncch0.bin
        --exh ${INTERNAL_DIR}/exheader_ncch0.bin
        --plain ${INTERNAL_DIR}/plain.bin
    DEPENDS
    Extract3DS
)

add_custom_target(ExtractSystemFiles ALL
    COMMAND
    ${ROM_TOOL} -xtfu exefs ${CMAKE_BINARY_DIR}/exefs.bin
        --exefs-dir ${ROM_DIR}/system
        --header ${INTERNAL_DIR}/header_system.bin
    DEPENDS
    ExtractGame
)

add_custom_target(ExtractRomFiles
    COMMAND
    ${ROM_TOOL} -xtf romfs ${CMAKE_BINARY_DIR}/romfs.bin
        --romfs-dir ${ROM_DIR}/data
    DEPENDS
    ExtractGame
)


# 2. Converter texts
file(MAKE_DIRECTORY "${MOD_DIR}/texts")

# 2.1 Episode and card texts
add_custom_target(CardInfo ALL
    COMMAND
    ${MONO} ${TOOLS_DIR}/AttackFridayMonsters.exe -e carddata0
        ${ROM_DIR}/data/gkk/cardgame/carddata.bin
        ${MOD_DIR}/texts/cardinfo.po
    DEPENDS
    ExtractRomFiles
)

add_custom_target(CardDialogs ALL
    COMMAND
    ${MONO} ${TOOLS_DIR}/AttackFridayMonsters.exe -e carddata25
        ${ROM_DIR}/data/gkk/cardgame/carddata.bin
        ${MOD_DIR}/texts/cardgame_dialogs.po
    DEPENDS
    ExtractRomFiles
)

add_custom_target(EpisodeTexts ALL
    COMMAND
    ${MONO} ${TOOLS_DIR}/AttackFridayMonsters.exe -e episode
        ${ROM_DIR}/data/gkk/episode/episode.bin
        ${MOD_DIR}/texts/episodes_title.po
    DEPENDS
    ExtractRomFiles
)

# 2.2 Scripts
file(MAKE_DIRECTORY "${MOD_DIR}/texts/scripts")
set(MAP_FILES
    data/gkk/map_gz/A01000.lz
    data/gkk/map_gz/A01100.lz
    data/gkk/map_gz/A02000.lz
    data/gkk/map_gz/A02100.lz
    data/gkk/map_gz/A03000.lz
    data/gkk/map_gz/A03001.lz
    data/gkk/map_gz/A03100.lz
    data/gkk/map_gz/A03101.lz
    data/gkk/map_gz/A04000.lz
    data/gkk/map_gz/A04100.lz
    data/gkk/map_gz/A04101.lz
    data/gkk/map_gz/A05000.lz
    data/gkk/map_gz/A05100.lz
    data/gkk/map_gz/A05101.lz
    data/gkk/map_gz/A06000.lz
    data/gkk/map_gz/A06100.lz
    data/gkk/map_gz/A07000.lz
    data/gkk/map_gz/A07100.lz
    data/gkk/map_gz/A08000.lz
    data/gkk/map_gz/A08001.lz
    data/gkk/map_gz/A08100.lz
    data/gkk/map_gz/A09000.lz
    data/gkk/map_gz/A09100.lz
    data/gkk/map_gz/A10000.lz
    data/gkk/map_gz/A10100.lz
    data/gkk/map_gz/A11000.lz
    data/gkk/map_gz/A11001.lz
    data/gkk/map_gz/A11100.lz
    data/gkk/map_gz/A12000.lz
    data/gkk/map_gz/A12100.lz
    data/gkk/map_gz/A13000.lz
    data/gkk/map_gz/A13100.lz
    data/gkk/map_gz/A14000.lz
    data/gkk/map_gz/A14100.lz
    data/gkk/map_gz/A15000.lz
    data/gkk/map_gz/A15100.lz
    data/gkk/map_gz/A16000.lz
    data/gkk/map_gz/A16100.lz
    data/gkk/map_gz/A17000.lz
    data/gkk/map_gz/A17100.lz
    data/gkk/map_gz/A18000.lz
    data/gkk/map_gz/A18001.lz
    data/gkk/map_gz/A18100.lz
    data/gkk/map_gz/A18101.lz
    data/gkk/map_gz/A19000.lz
    data/gkk/map_gz/A19100.lz
    data/gkk/map_gz/A20000.lz
    data/gkk/map_gz/A20100.lz
    data/gkk/map_gz/A21000.lz
    data/gkk/map_gz/A21100.lz
    data/gkk/map_gz/A21101.lz
    data/gkk/map_gz/A21102.lz
    data/gkk/map_gz/A21103.lz
    data/gkk/map_gz/A21104.lz
    data/gkk/map_gz/A21105.lz
    data/gkk/map_gz/A21106.lz
    data/gkk/map_gz/A21107.lz
    data/gkk/map_gz/A21108.lz
    data/gkk/map_gz/A22000.lz
    data/gkk/map_gz/A22100.lz
    data/gkk/map_gz/A22101.lz
    data/gkk/map_gz/A22102.lz
    data/gkk/map_gz/A22103.lz
    data/gkk/map_gz/A22104.lz
    data/gkk/map_gz/A22105.lz
    data/gkk/map_gz/A22106.lz
    data/gkk/map_gz/A23000.lz
    data/gkk/map_gz/A23100.lz
    data/gkk/map_gz/A24000.lz
    data/gkk/map_gz/A24100.lz
    data/gkk/map_gz/A25000.lz
    data/gkk/map_gz/A25001.lz
    data/gkk/map_gz/A25100.lz
    data/gkk/map_gz/A26000.lz
    data/gkk/map_gz/A26100.lz
    data/gkk/map_gz/A27000.lz
    data/gkk/map_gz/A27001.lz
    data/gkk/map_gz/A27100.lz
    data/gkk/map_gz/A27101.lz
    data/gkk/map_gz/A28000.lz
    data/gkk/map_gz/A28100.lz
    data/gkk/map_gz/A29000.lz
    data/gkk/map_gz/A29100.lz
    data/gkk/map_gz/A31000.lz
    data/gkk/map_gz/A31100.lz
    data/gkk/map_gz/A32000.lz
    data/gkk/map_gz/A32100.lz
    data/gkk/map_gz/A33000.lz
    data/gkk/map_gz/A33100.lz
    data/gkk/map_gz/A34000.lz
    data/gkk/map_gz/A34100.lz
    data/gkk/map_gz/A35000.lz
    data/gkk/map_gz/A35100.lz
    data/gkk/map_gz/A36000.lz
    data/gkk/map_gz/A36100.lz
    data/gkk/map_gz/A37000.lz
    data/gkk/map_gz/A37100.lz
    data/gkk/map_gz/A38000.lz
    data/gkk/map_gz/A38100.lz
    data/gkk/map_gz/A98000.lz
    data/gkk/map_gz/A99000.lz
    data/gkk/map_gz/B01000.lz
    data/gkk/map_gz/B01100.lz
    data/gkk/map_gz/B02000.lz
    data/gkk/map_gz/B02001.lz
    data/gkk/map_gz/B02100.lz
    data/gkk/map_gz/B03000.lz
    data/gkk/map_gz/B03001.lz
    data/gkk/map_gz/B03002.lz
    data/gkk/map_gz/B03100.lz
    data/gkk/map_gz/B04000.lz
    data/gkk/map_gz/B04001.lz
    data/gkk/map_gz/B04100.lz
    data/gkk/map_gz/B05000.lz
    data/gkk/map_gz/B05100.lz
    data/gkk/map_gz/B06000.lz
    data/gkk/map_gz/B06100.lz
    data/gkk/map_gz/B07000.lz
    data/gkk/map_gz/B07001.lz
    data/gkk/map_gz/B07100.lz
    data/gkk/map_gz/B07101.lz
    data/gkk/map_gz/B08000.lz
    data/gkk/map_gz/B08100.lz
    data/gkk/map_gz/B09000.lz
    data/gkk/map_gz/B09100.lz
    data/gkk/map_gz/B10000.lz
    data/gkk/map_gz/B10100.lz
    data/gkk/map_gz/B11000.lz
    data/gkk/map_gz/B11100.lz
    data/gkk/map_gz/B12000.lz
    data/gkk/map_gz/B12100.lz
    data/gkk/map_gz/B13000.lz
    data/gkk/map_gz/B13100.lz
    data/gkk/map_gz/B14000.lz
    data/gkk/map_gz/B14100.lz
    data/gkk/map_gz/B15000.lz
    data/gkk/map_gz/B15100.lz
    data/gkk/map_gz/B16000.lz
    data/gkk/map_gz/B16100.lz
    data/gkk/map_gz/B17000.lz
    data/gkk/map_gz/B17100.lz
    data/gkk/map_gz/B18000.lz
    data/gkk/map_gz/B18100.lz
    data/gkk/map_gz/B19000.lz
    data/gkk/map_gz/B19100.lz
    data/gkk/map_gz/B20000.lz
    data/gkk/map_gz/B20100.lz
    data/gkk/map_gz/C01000.lz
    data/gkk/map_gz/C02000.lz
    data/gkk/map_gz/C03000.lz
    data/gkk/map_gz/C04000.lz
    data/gkk/map_gz/C05000.lz
    data/gkk/map_gz/C06000.lz
    data/gkk/map_gz/C06100.lz
    data/gkk/map_gz/C07000.lz
    data/gkk/map_gz/C08000.lz
    data/gkk/map_gz/C09000.lz
    data/gkk/map_gz/C10000.lz
    data/gkk/map_gz/C11000.lz
    data/gkk/map_gz/C11001.lz
    data/gkk/map_gz/C11002.lz
    data/gkk/map_gz/C11100.lz
    data/gkk/map_gz/C12000.lz
    data/gkk/map_gz/C13000.lz
    data/gkk/map_gz/C14000.lz
    data/gkk/map_gz/C15000.lz
    data/gkk/map_gz/C15100.lz
    data/gkk/map_gz/C16000.lz
    data/gkk/map_gz/C17000.lz
    data/gkk/map_gz/C18000.lz
)
foreach(MAP_FILE ${MAP_FILES})
    set(FULL_MAP "${ROM_DIR}/${MAP_FILE}")
    get_filename_component(MAP_NAME "${MAP_FILE}" NAME_WE)
    set(SCRIPT_FILE "${MOD_DIR}/texts/scripts/${MAP_NAME}.po")

    list(APPEND TEXT_SCRIPT ${SCRIPT_FILE})
    add_custom_command(OUTPUT ${SCRIPT_FILE}
        COMMAND
        ${MONO} ${TOOLS_DIR}/AttackFridayMonsters.exe -e script ${FULL_MAP} ${SCRIPT_FILE}
        DEPENDS ExtractRomFiles
        WORKING_DIRECTORY ${TOOLS_DIR}
    )
endforeach()
add_custom_target(ExtractScripts ALL DEPENDS ${TEXT_SCRIPT})


# 3. Convert fonts
file(MAKE_DIRECTORY "${MOD_DIR}/fonts")
add_custom_target(Fonts ALL
    COMMAND
    ${MONO} ${TOOLS_DIR}/AttackFridayMonsters.exe -e darc
        ${ROM_DIR}/data/gkk/lyt/title.arc
        ${CMAKE_BINARY_DIR}/title
    COMMAND
    ${PYTHON_EXECUTABLE} ${TOOLS_DIR}/bcfnt.py -x -y
        -f ${CMAKE_BINARY_DIR}/title/font/kk_KN_Font.bcfnt
    WORKING_DIRECTORY ${MOD_DIR}/fonts
    DEPENDS
    ExtractRomFiles
)

# 4. Images
set(DARC_IMAGE_FILES
    data/gkk/cardgame/cardlyt_d.arc
    data/gkk/lyt/bumper.arc
    data/gkk/lyt/movie_low.arc
    data/gkk/lyt/notebook.arc
    data/gkk/lyt/selwin.arc
    data/gkk/lyt/title.arc
)
set(CARDLYT_D_IMAGES
    butji_mdr.bclim
    butji_n.bclim
    butji_y.bclim
    chan_moji.bclim
    dasu_butt.bclim
    mai_1.bclim
    mai_2.bclim
    mai_3.bclim
    mai_4.bclim
    mai_5.bclim
)
set(BUMPER_IMAGES
    ex_msg_A.bclim
    ex_msg_B.bclim
    ex_msg_C.bclim
    stereo_ji.bclim
)
set(MOVIE_LOW_IMAGES
    skip_ji.bclim
)
set(NOTEBOOK_IMAGES
    auto_moji.bclim
    butji_mdr.bclim
    page_00.bclim
    page_01.bclim
    page_02a.bclim
    page_02b.bclim
    page_03.bclim
    page_04.bclim
    page_05a.bclim
    page_05b.bclim
    page_06a.bclim
    page_06b.bclim
    page_07.bclim
    page_08.bclim
    page_09.bclim
    page_10.bclim
    page_11.bclim
    page_12.bclim
    page_13.bclim
    page_14.bclim
)
set(SELWIN_IMAGES
    butji_n.bclim
    butji_y.bclim
)
set(TITLE_IMAGES
    auto_mk.bclim
    butji_mdr.bclim
    butji_n.bclim
    butji_y.bclim
    epi_moji.bclim
    head_load.bclim
    head_save.bclim
    title_rogo.bclim
)

file(MAKE_DIRECTORY ${MOD_DIR}/images)
find_program(IMG_TOOL bclimtool)
foreach(DARC_FILE ${DARC_IMAGE_FILES})
    set(FULL_DARC "${ROM_DIR}/${DARC_FILE}")
    get_filename_component(DARC_NAME "${DARC_FILE}" NAME_WE)
    add_custom_command(
        OUTPUT
        ${CMAKE_BINARY_DIR}/${DARC_NAME}
        COMMAND
        ${MONO} ${TOOLS_DIR}/AttackFridayMonsters.exe -e darc
            ${FULL_DARC}
            ${CMAKE_BINARY_DIR}/${DARC_NAME}
        DEPENDS
        ExtractRomFiles
    )
    add_custom_target(Extract${DARC_NAME} DEPENDS ${CMAKE_BINARY_DIR}/${DARC_NAME})

    file(MAKE_DIRECTORY ${MOD_DIR}/images/${DARC_NAME})
    string(TOUPPER ${DARC_NAME} DARC_NAME_UPPER)
    foreach(IMG_FILE ${${DARC_NAME_UPPER}_IMAGES})
        set(FULL_IMG ${CMAKE_BINARY_DIR}/${DARC_NAME}/timg/${IMG_FILE})
        get_filename_component(IMG_NAME ${IMG_FILE} NAME_WE)
        add_custom_command(
            OUTPUT
            ${MOD_DIR}/images/${DARC_NAME}/${IMG_NAME}.png
            COMMAND
            ${IMG_TOOL} -dfp "${FULL_IMG}" ${MOD_DIR}/images/${DARC_NAME}/${IMG_NAME}.png
            WORKING_DIRECTORY
            ${TOOLS_DIR}
            DEPENDS
            Extract${DARC_NAME}
        )
        list(APPEND IMAGES ${MOD_DIR}/images/${DARC_NAME}/${IMG_NAME}.png)
    endforeach()
endforeach()
add_custom_target(ExtractImages ALL DEPENDS ${IMAGES})
