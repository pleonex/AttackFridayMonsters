#  Copyright (c) 2020 Benito Palacios Sanchez
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

###############################################################################
# liblzma
###############################################################################
set(LIBLZMA_INSTALL_DIR "${CMAKE_CURRENT_BINARY_DIR}/xz-install")
file(MAKE_DIRECTORY "${LIBLZMA_INSTALL_DIR}/include")
ExternalProject_add(xz
    SOURCE_DIR "${CMAKE_CURRENT_SOURCE_DIR}/xz"
    INSTALL_DIR "${LIBLZMA_INSTALL_DIR}"
    CMAKE_CACHE_ARGS
        -DCMAKE_BUILD_TYPE:STRING=$<CONFIG>
        -DCMAKE_INSTALL_PREFIX:PATH=<INSTALL_DIR>
    BUILD_COMMAND
        cmake --build <BINARY_DIR> --config $<CONFIG>
    INSTALL_COMMAND
        cmake --build <BINARY_DIR> --config $<CONFIG> --target install
)

# Since the export target is generated at build time we can't just import it
# it's easier if we replicate its content here.
set(THREADS_PREFER_PTHREAD_FLAG TRUE)
find_package(Threads REQUIRED)

add_library(liblzma::liblzma STATIC IMPORTED)
add_dependencies(liblzma::liblzma xz)
set_target_properties(liblzma::liblzma PROPERTIES
    INTERFACE_COMPILE_DEFINITIONS "LZMA_API_STATIC"
    INTERFACE_INCLUDE_DIRECTORIES "${LIBLZMA_INSTALL_DIR}/include"
    INTERFACE_LINK_LIBRARIES "Threads::Threads"
)
set_property(TARGET liblzma::liblzma APPEND PROPERTY IMPORTED_CONFIGURATIONS DEBUG)
set_target_properties(liblzma::liblzma PROPERTIES
    IMPORTED_LINK_INTERFACE_LANGUAGES_DEBUG "C"
    IMPORTED_LOCATION_DEBUG "${LIBLZMA_INSTALL_DIR}/lib/liblzma.lib"
)
set_property(TARGET liblzma::liblzma APPEND PROPERTY IMPORTED_CONFIGURATIONS RELEASE)
set_target_properties(liblzma::liblzma PROPERTIES
    IMPORTED_LINK_INTERFACE_LANGUAGES_RELEASE "C"
    IMPORTED_LOCATION_RELEASE "${LIBLZMA_INSTALL_DIR}/lib/liblzma.lib"
)

###############################################################################
# XDelta
###############################################################################
check_type_size("size_t" SIZEOF_SIZE_T)
check_type_size("unsigned long long" SIZEOF_UNSIGNED_LONG_LONG)

set(XDELTA_SOURCE_DIR "${CMAKE_CURRENT_SOURCE_DIR}/xdelta/xdelta3")
set(XDELTA_HEADERS
    "${XDELTA_SOURCE_DIR}/xdelta3-blkcache.h"
    "${XDELTA_SOURCE_DIR}/xdelta3-decode.h"
    "${XDELTA_SOURCE_DIR}/xdelta3-djw.h"
    "${XDELTA_SOURCE_DIR}/xdelta3-fgk.h"
    "${XDELTA_SOURCE_DIR}/xdelta3-hash.h"
    "${XDELTA_SOURCE_DIR}/xdelta3-internal.h"
    "${XDELTA_SOURCE_DIR}/xdelta3-list.h"
    "${XDELTA_SOURCE_DIR}/xdelta3-lzma.h"
    "${XDELTA_SOURCE_DIR}/xdelta3-main.h"
    "${XDELTA_SOURCE_DIR}/xdelta3-merge.h"
    "${XDELTA_SOURCE_DIR}/xdelta3-second.h"
    # "${XDELTA_SOURCE_DIR}/xdelta3-test.h"
    "${XDELTA_SOURCE_DIR}/xdelta3-cfgs.h"
    "${XDELTA_SOURCE_DIR}/xdelta3.h"
)
set(XDELTA_SOURCES
    "${XDELTA_SOURCE_DIR}/xdelta3.c"
)
add_executable(xdelta3 ${XDELTA_HEADERS} ${XDELTA_SOURCES})
target_link_libraries(xdelta3 liblzma::liblzma)
target_compile_definitions(xdelta3
    PRIVATE
        SIZEOF_SIZE_T=${SIZEOF_SIZE_T}
        SIZEOF_UNSIGNED_LONG_LONG=${SIZEOF_UNSIGNED_LONG_LONG}
        XD3_DEBUG=$<BOOL:$<CONFIG:Debug>>
        XD3_WIN32=$<BOOL:${WIN32}>
        XD3_POSIX=$<BOOL:${UNIX}>
        XD3_STDIO=0             # Other systems than Win or Unix, unclear which ones
        XD3_MAIN=1              # Build as app
        NOT_MAIN=0              # Build as app, no need of lib custom main
        XD3_ENCODER=1           # Build encoder also
        REGRESSION_TEST=1       # Build command to run built-in tests
        XD3_USE_LARGEFILE64=1   # Support files larger than 4 GB
        SECONDARY_DJW=1
        SECONDARY_FGK=1
        SECONDARY_LZMA=1
        EXTERNAL_COMPRESSION=$<BOOL:${UNIX}>  # Support external only for Unix
        SHELL_TESTS=0           # Not compiling test code
)
install(TARGETS xdelta3 DESTINATION tools)
