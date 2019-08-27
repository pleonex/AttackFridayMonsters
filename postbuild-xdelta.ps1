$ErrorActionPreference = "Stop"

Set-Location GameData\full

Write-Host "Generating NCCH"
& ..\tools\3dstool.exe -ctf cxi .\game.bin `
    --exefs ..\output\game.3ds.exefs `
    --romfs ..\output\game.3ds.romfs `
    --exh .\exheader_ncch0.bin `
    --plain .\plain.bin `
    --header .\header_ncch0.bin `

Write-Host "Generating NCSD"
& ..\tools\3dstool.exe -ct01f cci `
    .\game.bin .\manual.bin `
    modified.3ds `
    --header .\header_cci.bin

Write-Host "Generating patch"
& ..\tools\xdelta3.exe -e -s ..\game.3ds modified.3ds ..\output\game.xdelta

Set-Location ..\..\
