# SimpleCompress
A multi-file compression utility for directoies with long paths and many duplicate files

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
        x : ( pack ) create an expander script for the archive


