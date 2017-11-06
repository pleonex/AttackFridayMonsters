# DARC
The *DARC* format is for containers of any kind of files. It seems to be a common format from the SDK of Nintendo. The extension is usually *.arc*.

## Format
Offset | Size | Description
------ | ---- | -----------
0x00   | 4    | Magic stamp `darc`
0x04   | 2    | Endianness
0x06   | 4    | Header size
0x0A   | 2    | Version (`0x0100`)
0x0C   | 4    | File size
0x10   | 4    | FAT offset
0x14   | 4    | FAT size
0x18   | 4    | Data offset
0x1C   | ...  | FAT
...    | ...  | Files

### FAT
It consist of a block of 0xC bytes per entry, followed by null-terminated names. An entry can be a file or a folder. The entry format is:

Offset | Size | Description
------ | ---- | -----------
0x00   | 3    | Name relative offset to the end of entries
0x03   | 1    | Entry type: 0=file, 1=folder
0x04   | 4    | [File: absolute offset, Folder: null]
0x08   | 4    | [File: size, Folder: number of files and folders]

The root folder entry is special. The name offset is null and the number of elements is actually the total name of files and folders. From this number we can get the start of the name section by multiplying by the size of an entry (12 bytes).
