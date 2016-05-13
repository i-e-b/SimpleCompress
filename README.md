# SimpleCompress
Multi-file compression with long path support and good compression with many duplicate files

The C# version specifically uses only standard GAC dependencies so it can be included in .msi installers.

**Important** This tool does not keep track of file permissions, flags, creation dates or any other
meta-data. It will lose execute flags.

Symlinks are supported for directories. If a link target is outside the archive, it will be dereferenced
and expanded to a normal file. If the link target is within the archive, it will be restored as a link.

The npm package can be installed as a library, or as a CLI tool "sz" with `npm i -g simple-compress`.

    Usage:
        sz pack <src directory> <target file> [flags]
        sz unpack <src file> <target directory> [flags]

    Flags:
        h : (unpack) replace duplicate files with hard links
        x : (pack) create an expander script for the archive

### Internals

Filesystem / IO:

The .Net version uses a cut down version of https://github.com/i-e-b/tinyQuickIO to handle long file paths in Windows.
The built in .Net IO namespace often fails with `node_modules` folders in large projects.
Note that long file names are not currently supported for compressed and temp files.

Compression:

It works like a tar-gz: First all files are checked for duplicates
(if it has the same name and MD5 it is considered a duplicate).
Then a temp file is written that is of the structure `<MD5:16 bytes><length:8 bytes><paths:utf8 str><length:8 bytes><data:byte array>`
repeated for each unique file. Lengths are 64 bit little endian. Paths are UTF-8. Data is as in the source file.
This temp file is then compressed to a single gzip stream as the final output (it is **not** a .zip file)

Decompression:

Reverse of compression -- first the gzip stream is expanded out to a temp file, then the structure is read to get a
list of paths. The data is then read to disk at first path in the list, then that result file is copied around to all
the other locations. Each file when written can be compared to the archive's MD5 hash and an error displayed if there
is any corruption.

