// Node version of the .Net simple compress app

var fs = require('fs');
var zlib = require('zlib');
var path = require('path');
var crypto = require('crypto')

var args = process.argv.slice(2);
if (args.length != 3) { ShowUsageAndExit(); }

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
// end of main program
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

function ShowUsageAndExit() {
    console.log("Simple Compress\r    Usage:\r        sc pack <src directory> <target file>\r        sc unpack <src file> <target directory>");
    process.exit(1);
}

// recursively scan a directorf and pack its contents into an archive
function Pack(src, dst) {
    var temp = src + '.tmp';
    if (fs.existsSync(temp)) {fs.truncateSync(temp, 0);}

    // build dictionary of equal files
    // open pack file
    // write first data of each dict entry to cat file
    // close and gzip the cat file
    // delete cat file
}

// async hash of file.
// TODO: convert to sync without having to read whole file.
function fileHash (filename, callback) {
  var sum = crypto.createHash('md5')
  if (callback && typeof callback === 'function') {
    var fileStream = fs.createReadStream(filename)
    fileStream.on('error', function (err) {
      return callback(err, null)
    })
    fileStream.on('data', function (chunk) {
      try {
        sum.update(chunk)
      } catch (ex) {
        return callback(ex, null)
      }
    })
    fileStream.on('end', function () {
      return callback(null, sum.digest('hex'))
    })
  } else {
    sum.update(fs.readFileSync(filename))
    return sum.digest('hex')
  }
}

// expand an existing package file into a directory structure
function Unpack(src, dst) {
    var temp = src + '.tmp';
    // Unzip archive to temp location
    if (fs.existsSync(temp)) {fs.truncateSync(temp, 0);}
    var gzip = zlib.createGunzip();
    var inp = fs.createReadStream(src);
    var out = fs.createWriteStream(temp);

    console.log('decompressing');
    var unzip = inp.pipe(gzip).pipe(out); // unpack into catenated file

    unzip.on('finish', function unzipCallback(){
        if (inp.end) inp.end()
            if (out.end) out.end();

        console.log('unpacking files');
        unpackCat(temp, dst);
        console.log('done');
    });
}

// then run this over the structure...
// ToDo: see if this can be re-written as a single pipeline

function unpackCat(srcPack, targetPath) {
    var cat = fs.openSync(srcPack, 'r');
    var buf = new Buffer(65536);
    var offset = 0;

    for(;;){
        var pathLen = ReadLength(cat, buf);
        if (pathLen == 0) {break;}
        var paths = ReadPaths(pathLen, cat, buf);

        // read contents out to all the files at once.
        // The .Net version does a write-then-copy, but Node.js has no OS-level copy.
        var fileLen = ReadLength(cat, buf);
        ReadToFiles(fileLen, paths, targetPath, cat, buf);
    }
    fs.close(cat);
    fs.unlinkSync(srcPack);
}

function ReadPaths(len, fd, buffer){
    // todo: handle paths > 64k long
    var rlen = fs.readSync(fd, buffer, 0, len, null);
    if (rlen < 1) throw new Error('Malformed file: Empty path set');
    if (rlen != len) throw new Error("Malformed file: truncated file path list");
    var s = buffer.toString('utf8',0,rlen);
    return s.split('|');
}

function ReadToFiles(len, paths, target, fd, buffer){
    var remains = len;
    // check directories and truncate files
    for (var i = 0; i < paths.length; i++){
        var npath = path.join(target, paths[i]);
        ensureDirectory(path.dirname(npath));
        if (fs.existsSync(npath)) {fs.truncateSync(npath, 0);}
    }

    // read bytes into files
    while (remains > 0){
        var next = (remains > buffer.length) ? (buffer.length) : (remains);
        var rlen = fs.readSync(fd, buffer, 0, next, null);
        if (rlen < 1) throw new Error('Malformed file: truncated file data');
        remains -= rlen;

        var data = buffer.slice(0, rlen);
        for (var i = 0; i < paths.length; i++){
            var npath = path.join(target, paths[i]);
            fs.appendFileSync(npath, data);
        }
    }
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
};

