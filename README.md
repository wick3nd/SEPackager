# SEP packager
SEP packager is a packager for files for a future game engine called SteelEngine

Both the packager and the engine is a work in progress

# Format specification
The archive is divided to 2 sections - directory archive and the data archive

## Directory Archive
The directory archive structure is specified as below

### Header
```
0        1        2        3        4        5        6        7        8        9        10       11       12       13       14
+--------+--------+--------+--------+--------+--------+--------+--------+--------+--------+--------+--------+--------+--------+
| Identifier                                                            | V. maj | V. min | Mode   | File Count      | CRC    |
+--------+--------+--------+-----------------+--------+-----------------+--------+--------+--------+-----------------+--------+
```

### Sectors
```
0        1        2        3        4
+--------+--------+--------+--------+
| P. Len | Path         ...| Pointer| 
+--------+--------------------------+
| Offset                            |
+-----------------------------------+
| Length                            |
+-----------------------------------+
| Original Length (Mode 0 only)     |
+-----------------+-----------------+
| CRC             |
+-----------------+ 
```

## Data Archive
The data archive structure is specified as below

### Header
```
0        1        2        3        4        5        6        7        8        9        10       11
+--------+--------+--------+--------+--------+--------+--------+--------+--------+--------+--------+
| Identifier                                                            | V. maj | V. min | CRC    |
+--------+--------+--------+-----------------+--------+-----------------+--------+--------+--------+
```

### Data
The data is stored depending on the mode of the archive.

## CRC
|     | DirArchive Header | DataArchive Header | DirArchive Sector | DataArchive Data |
|-----|-------------------|--------------------|-------------------|------------------|
| CRC | CRC8 ATM-2        | CRC8 ATM-2         | CRC16 CCITT False | CRC32            |

## Compression
The compression is dependent on the mode of the archive. The function of different modes is listed below.
| Mode | Use Case      | Compression | Compression Type |
|------|---------------|-------------|------------------|
| 0x00 | Miscellaneous | Zstd        | 5                |
| 0x01 | Images        | Custom      | SteelEngine Ready|
| 0x02 | Sound         | None        | None             |
