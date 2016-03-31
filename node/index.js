// Node version of the .Net simple compress app

var fs = require('fs');
var zlib = require('zlib');
var path = require('path');
var crypto = require('crypto')

var isWin = /^win/.test(process.platform);

module.exports = {
    cli: cli,
    pack: Pack,
    unpack: Unpack
};

function cli() {
    var args = process.argv.slice(2);
    if (args.length != 3) { return ShowUsageAndExit(); }

    var src = path.resolve(args[1]);
    var dst = path.resolve(args[2]);

    switch (args[0]) {
        case "pack":
            Pack(src, dst);
            break;

        case "unpack":
            Unpack(src, dst);
            break;

        default:
            ShowUsageAndExit();
            break;
    }
}
// end of main program
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

function ShowUsageAndExit() {
    console.log(
        ["Simple Compress",
            "    Usage:",
            "        sz pack <src directory> <target file>",
            "        sz unpack <src file> <target directory>"].join(require('os').EOL)
    );
}

var logStageWaiting = false;
function logStage(str) {
    if (logStageWaiting) {console.timeEnd(' done');}
    process.stdout.write(str);
    console.time(' done');
    logStageWaiting = true;
}

// recursively scan a directory and pack its contents into an archive
function Pack(src, dst) {
    var temp = dst + '.tmp';
    if (fs.existsSync(temp)) {fs.truncateSync(temp, 0);}

    // build dictionary of equal files
    logStage('scanning for duplicates');
    var parts = {};
    var links = {};
    applyToDeepPaths(src, function eachFile (p){
        var hash = fileHashSync(p);
        var key = path.basename(p)+'|'+(hash.toString('base64'));
        var subs = p.replace(src,"");
        if (parts[key]) parts[key].paths.push(subs);
        else parts[key] = {paths : [subs], hash : hash};
    }, function shouldFollowSymlink(srcPath, targetPath){
        if (targetPath.indexOf(src) === 0) {
            // the link targets a path inside our source, write a link and return false (no follow)
            links[srcPath.replace(src,"")] = targetPath.replace(src,"");
            return false;
        } else {
            // the link is outside, we just return true (follow and treat as if it was not a link)
            return true;
        }
    });

    // open pack file
    var cat = fs.openSync(temp, 'w');

    logStage('writing log');
    // write first data of each dict entry to cat file
    Object.keys(parts).forEach(function(key){
        // Write <MD5:16 bytes>
        WriteBuffer(cat, parts[key].hash, 16);
        // Write <length:8 bytes><paths:utf8 str>
        var paths = parts[key].paths;
        var pathBuffer = new Buffer(paths.join('|'), 'utf8');
        WriteLength(cat, pathBuffer.length);
        fs.writeSync(cat, pathBuffer, 0, pathBuffer.length, null);
        // Write <length:8 bytes><data:byte array>
        var srcFile = path.resolve(path.join(src, paths[0]));
        var fileSize = fs.statSync(srcFile).size;
        WriteLength(cat, fileSize);
        WriteFileData(cat, srcFile);
    });
    // write link table
    Object.keys(links).forEach(function(key){
        // Write <zeroes:16 bytes>
        WriteLinkMarker(cat);

        // Write <length:8 bytes><path pair:utf8 str>, path pair is 'src|target'
        var pathBuffer = new Buffer(key+'|'+links[key], 'utf8');
        WriteLength(cat, pathBuffer.length);
        fs.writeSync(cat, pathBuffer, 0, pathBuffer.length, null);

        // Write <length:8 bytes>, always zero (there is not file content in a link)
        WriteLength(cat, 0);
    });

    // close and gzip the cat file
    fs.close(cat);
    if (fs.existsSync(dst)) {fs.truncateSync(dst, 0);}
    var gzip = zlib.createGzip({level:9});
    var inp = fs.createReadStream(temp);
    var out = fs.createWriteStream(dst);

    logStage('compressing');
    var unzip = inp.pipe(gzip).pipe(out); // unpack into catenated file

    // delete cat file
    unzip.on('finish', function unzipCallback(){
        if (inp.end) inp.end();
        if (out.end) out.end();
        // TODO: temp -> fs.unlinkSync(temp);
        logStage('');
    });
}

function WriteLength(fd, length){
    var b = new Buffer(8);
    b.writeIntLE(length, 0, 8);
    fs.writeSync(fd, b, 0, 8, null);
}

function WriteLinkMarker(fd){
    // 16 bytes of zeroes instead of MD5
    var b = new Buffer(16);
    b.fill(0);
    fs.writeSync(fd, b, 0, 16, null);
}

function WriteBuffer(fd, buf, len) {
    fs.writeSync(fd, buf, 0, len, null);
}

function WriteFileData(dst, fileToAdd){
    var src = fs.openSync(fileToAdd, 'r');
    var buf = new Buffer(65536);

    for (;;){
        var rlen = fs.readSync(src, buf, 0, buf.length, null);
        if (rlen < 1) break;
        fs.writeSync(dst, buf, 0, rlen, null);
    }
    fs.close(src);
}

// run a function against every path under a root path
function applyToDeepPaths(root, fileFunction, followSymlinkPredicate){
    var exists = fs.existsSync(root);
    var stats = exists && fs.lstatSync(root);
    if (stats.isSymbolicLink()) {
        var target = fs.readlinkSync(root);
        if (followSymlinkPredicate && !followSymlinkPredicate(root, target)) return;
        stats = fs.statSync(root); // follow the link
    }
    var isDirectory = exists && stats.isDirectory();
    if (isDirectory) {
        fs.readdirSync(root).forEach(function(childItemName) {
            applyToDeepPaths(path.join(root, childItemName), fileFunction, followSymlinkPredicate);
        });
    } else if (exists) {
        fileFunction(root);
    }
}

// sync file hash without loading whole file
function fileHashSync(filename){
    var sum = crypto.createHash('md5');
    var fd = fs.openSync(filename, 'r');
    var buf = new Buffer(65536);

    for (;;){
        var rlen = fs.readSync(fd, buf, 0, buf.length, null);
        if (rlen < 1) break;
        sum.update(buf.slice(0, rlen));
    }
    fs.close(fd);
    return sum.digest();
}

// expand an existing package file into a directory structure
function Unpack(src, dst) {
    var temp = src + '.tmp';
    // Unzip archive to temp location
    if (fs.existsSync(temp)) {fs.truncateSync(temp, 0);}
    var gzip = zlib.createGunzip();
    var inp = fs.createReadStream(src);
    var out = fs.createWriteStream(temp);

    logStage('decompressing');
    var unzip = inp.pipe(gzip).pipe(out); // unpack into catenated file

    unzip.on('finish', function unzipCallback(){
        if (inp.end) inp.end();
        if (out.end) out.end();

        logStage('unpacking files');
        unpackCat(temp, dst);
        logStage('');
    });
}

// then run this over the structure...
// ToDo: see if this can be re-written as a single pipeline

function unpackCat(srcPack, targetPath) {
    var cat = fs.openSync(srcPack, 'r');
    var buf = new Buffer(5000000); // general purpose buffer. Gets overwritten by child functions
    var offset = 0;

    for(;;){
        var hash = ReadHash(cat);
        if (hash == null) {break;}
        var pathLen = ReadLength(cat, buf);
        if (pathLen == 0) {break;}
        var paths = ReadPaths(pathLen, cat, buf);
        var fileLen = ReadLength(cat, buf);

        if (fileLen == 0 && hash.toString('hex') == "00000000000000000000000000000000") { // is a symlink to be restored
            if (paths.length != 2) { throw new Error('Malformed file: symbolic link did not have a single source and target'); }
            fs.symlinkSync(/*target*/path.join(targetPath, paths[1]), /*source*/path.join(targetPath, paths[0]), 'dir');
        } else { // is file data to be written to paths
            // read contents out to all the files at once.
            // The .Net version does a write-then-copy, but Node.js has no OS-level copy.
            ReadToFiles(fileLen, paths, targetPath, cat, buf, hash);
        }
    }
    fs.close(cat);
    fs.unlinkSync(srcPack);
}

function ReadHash(fd) {
    var hbuf = new Buffer(16);
    var rlen = fs.readSync(fd, hbuf, 0, 16, null);
    if (rlen == 0) return null; // no more file
    if (rlen !== 16) throw new Error("Malformed file: truncated file set checksum");
    return hbuf;
}

function ReadPaths(len, fd, buffer){
    // todo: handle paths longer than buffer?
    var rlen = fs.readSync(fd, buffer, 0, len, null);
    if (rlen < 1) throw new Error('Malformed file: Empty path set');
    if (rlen != len) throw new Error("Malformed file: truncated file path list");
    var s = buffer.toString('utf8',0,rlen);
    s = correctFilepath(s);
    return s.split('|');
}

function ReadToFiles(len, paths, target, fd, buffer, expectedHash){
    var remains = len;

    if (paths.length < 1) return;

    // check directories and truncate files
    for (var i = 0; i < paths.length; i++){
        var npath = path.join(target, paths[i]);
        ensureDirectory(path.dirname(npath));
        if (fs.existsSync(npath)) {fs.truncateSync(npath, 0);}
    }

    // read bytes into files. We write one file at a time, rescanning the buffer.
    // it should be less efficient, but it plays nicely with anti-virus junkware
    // and OS expectations, resulting in a faster overall output

    // 1) write first file
    var rlen = 0;
    var masterPath = path.join(target, paths[0]);
    var masterFd = fs.openSync(masterPath, 'w+');
    var sum = crypto.createHash('md5');
    while (remains > 0){
        var next = (remains > buffer.length) ? (buffer.length) : (remains);
        rlen = fs.readSync(fd, buffer, 0, next, null);
        if (rlen < 1) throw new Error('Malformed file: truncated file data');
        remains -= rlen;
        // write the file
        fs.writeSync(masterFd, buffer, 0, rlen);
        // calculate hash
        sum.update(buffer.slice(0, rlen));
    }
    var actualHash = sum.digest();

    // Check the written data against the originam checksum
    // We do this only once, because we're checking for errors in the package transit, not errors of the local system.
    if (actualHash.toString() !== expectedHash.toString()) {
        throw new Error('Damaged archive: File at '+paths[0]+' failed a checksum');
    }

    // 2) copy original for all subsequent files. Unfortunately this is slow under nodejs
    for (var i = 1; i < paths.length; i++){
        var npath = path.join(target, paths[i]);
        var dstFd = fs.openSync(npath, 'w');
        var pos = 0;
        while (pos < len) {
            rlen = fs.readSync(masterFd, buffer, 0, buffer.length, pos);
            if (rlen < 1) break;
            pos += rlen;
            fs.writeSync(dstFd, buffer, 0, rlen);
        }
        fs.close(dstFd);
    }
    fs.close(masterFd);
}

function ReadLength(fd, buffer){
    var rlen = fs.readSync(fd, buffer, 0, 8, null);
    if (rlen == 0) return 0; // correct end-of-file
    if (rlen != 8) throw new Error("Malformed file: truncated file segment length");
    return buffer.readIntLE(0, 8); // Node can only support 48 bits of precision, but the file uses 64 bits of data.
}

// node's file system support is shocking
function ensureDirectory (p) {
    p = path.resolve(p);
    try {
        fs.mkdirSync(p);
    } catch (err0) {
        switch (err0.code) {
            case 'ENOENT' :
                ensureDirectory(path.dirname(p));
                ensureDirectory(p);
                break;
            default:
                try {
                    var stat = fs.statSync(p);
                    if (!stat.isDirectory()) throw err0;
                } catch (err1) {
                    throw err0;
                }
                break;
        }
    }
}

/**
 * The compression may have been done on a different os to the current one, so the paths may need to be corrected
 * @param path
 */
function correctFilepath(path) {
    if (isWin) return path.split('/').join('\\');
    else return path.split('\\').join('/');
}
