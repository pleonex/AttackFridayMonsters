# OFS3 (Object File System 3)
*OFS3* is a format type for containers with any kind of files. In this game this format uses the *.bin* extension.

## Format
Offset | Size | Description
------ | ---- | -----------
0x00   | 0x10 | Header
0x10   | ...  | FAT
...    | ...  | Children data
...    | ...  | File names if present

### Header
Offset | Size | Description
------ | ---- | -----------
0x00   | 4    | Magic stamp (`OFS3`)
0x04   | 4    | Header size, always 0x10
0x08   | 2    | FAT type (known values: 0, 1, 2)
0x0A   | 2    | Padding value
0x0C   | 4    | FAT + Children data sizes

### FAT
the *File Allocation Table* (FAT) section contains the information about the offset, size and name of the child files.

It starts with a value with the number of files. Then, for each file there is a 8 byte entry with 2 values:
* File offset (without header)
* File size

If FAT type from the header is 2, then the entry is 12 bytes and it contains an additional field with the offset to the null-terminated name of the file.

Offset | Size | Description
------ | ---- | -----------
0x00   | 4    | Number of files
0x04   | 4    | File 0 offset
0x08   | 4    | File 0 size
0x0C   | 4    | **Only if FAT is 2**: File 0 name offset
...    | ...  | Information for the rest of files
