# Script
The scripts are binary files that define the behavior of an scene. The files are inside the folder *map_gz*, for instance *A0100.lz*. This file is compressed with the LZX algorithm. You can use the tool LZX from CUE to decompress and compress them. The output file is an [OFS3](OFS3.md) file with the content of the map. The second child is the script of the map.

## Format
Offset | Size | Description
------ | ---- | -----------
0x00   | 4    | Number of blocks
...    | ...  | Block info
...    | ...  | Blocks

For each block there are three 32-bits integers (12 bytes) with the information of the block.
1. Unknown
2. Block size
3. Block offset

Blocks are padded to 0x10.

### Blocks
Offset | Size | Description
------ | ---- | -----------
0x00   | 4    | Number of sections
...    | ...  | Section info
...    | ...  | Sections

There are a 32-bits integer (4 bytes) per section with its *offset*.
Each section is padded to 4.

##### Sections
The meaning of each section seems to be fixed:

Section | Description
------  | -----------
1       | Unknown
2       | Unknown
3       | Unknown
4 - end | Dialogues
