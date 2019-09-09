# `Attack of the Friday Monsters!` Spanish Translation Tools

Tools for the Spanish fan-translation of the 3DS video-game: *Attack of the Friday Monsters! A Tokyo Tale*. By [GradienWords](https://gradienwords.github.io).

## Tools

### Dependencies

These tools use different languages like C, C# and Python. To compile and use them you need the following prorams:

* CMake: >= 3.0
* [*Windows*] .NET Framework 4.6.1
* [*Linux/Mac OS X*] Mono 5.4
* Python: 3.7
* gcc / clang / Visual Studio

### Compile

You can use CMake to compile the tools as follows:

```cmake
cd Programs
mkdir build; cd build
cmake .. -DCMAKE_INSTALL_PREFIX=../../GameData
cmake --build . --target install
```

The tools will be under *GameData/tools*.

## Export files

Run `build.ps1` on Windows or `build.sh` on Linux / Mac OS with the argument
`-script exporter.cake`.

The script expects to find the game under `GameData/game.3ds` but you can specify
another location with the `--game=<path>` argument.

The files will be exported into `GameData/extracted` or any other location specified
in the `--output` argument.

## Import files

Run `build.ps1` on Windows or `build.sh` on Linux / Mac OS with the argument
`-script importer.cake`.

The script expects to find the game under `GameData/game.3ds` but you can specify
another location with the `--game=<path>` argument.

The script expects to find the translated files under `Spanish/es` but you can specify
another location with the `--translation=<path>` argument.

The patched files will be exported into `GameData/luma` or any other location specified
in the `--luma` argument. This folder can be copied & pasted directly into your
SD card. Alternatively, you can find the generated RomFS and ExeFs in the output
directory.

## Credits

* pleonex:
  * [Yarhl](https://github.com/SceneGate/yarhl)
  * [Lemon](https://github.com/SceneGate/Lemon)
  * Custom game converter and formats.
* CUE:
  * [blz (de)compressor](https://www.romhacking.net/utilities/826/).
* ObsidianX:
  * [Font tool](https://github.com/ObsidianX/3dstools)
* dnasdw:
  * [3DS generator](https://github.com/dnasdw/3dstool)
  * [bclim image tool](https://github.com/dnasdw/bclimtool)
  * [texture image tool](https://github.com/dnasdw/txobtool)
* jmacd:
  * [xdelta](https://github.com/jmacd/xdelta-gpl)
