# SimpleCompress
An quick test of multi-file compression with long paths and many duplicate files

### To do

 * handle long file names

### Internals

Compression:

It works like a tar-gz: First all files are checked for duplicates
(if it has the same name and MD5 it is considered a duplicate).
Then a temp file is written that is of the structure `<length><paths><length><data>` repeated for each unique file. Lengths are 64 bit little endian. Paths are UTF-8. Data is as in the source file.

