# Attack of the Friday Monsters! Spanish Translation
Tools for the Spanish fan-translation of the 3DS video-game: *Attack of the Friday Monsters! A Tokyo Tale*. By [GradienWords](http://gradienwords.es).

## Dependencies
These tools use different languages like C, C# and Python. To compile and use them you need the following prorams:
* CMake: >= 3.0
* [*Windows*] .NET Framework 4.6.1
* [*Linux/Mac OS X*] Mono 5.4
* Python: 2.7 / 3.x
* gcc / clang / Visual Studio


## Generate tools
You can use CMake to compile the tools as follows:
```cmake
cd Programs
mkdir build; cd build
cmake .. -DCMAKE_INSTALL_PREFIX=../../GameData
cmake --build . --target install
```

The tools will be under *GameData/tools*.

## Decompile
You can get the files to translate using CMake as follows:
```cmake
cd Decompiler
mkdir build; cd build
cmake ..
cmake --build .
```

The files will be under *Decompiler/mod*. CMake variables to change:
* `DECOMPILER_ROM_FILE`: ROM file to decompile
* `DECOMPILER_TOOLS_DIR`: Decompilation tools
* `DECOMPILER_PREFIX`: Root directory for decompilation
* `DECOMPILER_ROM_DIR`: Directory to store ROM files
* `DECOMPILER_MOD_DIR`: Directory to store modification files
* `DECOMPILER_ROM_INTERNAL_DIR`: Directory to store ROM internal data for compilation

## Compile
You can generate a new ROM using CMake as follows:
```cmake
NOT IMPLEMENTED YET
```
