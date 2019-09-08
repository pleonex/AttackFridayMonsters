$ErrorActionPreference = "Stop"

Write-Host "Generating NCCH"
Remove-Item -ErrorAction Ignore GameData\output\program.bin
& GameData\tools\3dstool.exe -ctf cxi GameData\output\program.bin `
    --exefs GameData\output\game.3ds.exefs `
    --romfs GameData\output\game.3ds.romfs `
    --exh GameData\rom_metadata\exheader_ncch0.bin `
    --plain GameData\rom_metadata\plain.bin `
    --header GameData\rom_metadata\header_ncch0.bin

Write-Host "Generating NCSD"
Remove-Item -ErrorAction Ignore GameData\output\game_patched.3ds
& GameData\tools\3dstool.exe -ct01f cci `
    GameData\output\program.bin GameData\rom_metadata\manual.bin `
    GameData\output\game_patched.3ds `
    --header GameData\rom_metadata\header_cci.bin

Write-Host "Generating patch"
Remove-Item -ErrorAction Ignore GameData\output\patch.xdelta
& GameData\tools\xdelta3.exe -e -S `
    -s GameData\game.3ds `
    GameData\output\game_patched.3ds `
    GameData\output\patch.xdelta
