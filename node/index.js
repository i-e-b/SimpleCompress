// Node version of the .Net simple compress app

var fs = require('fs');
var zlib = require('zlib');
var path = require('path');

// to start with, unpack a presupplied filesystem:
var inputFile = 'C:/Temp/sample.inpkg';
var outputPath = 'C:/Temp/nodeOut/';
var temp = inputFile + '.tmp';

// Unzip archive to temp location
if (fs.existsSync(temp)) {fs.truncateSync(temp, 0);}
var gzip = zlib.createGunzip();
var inp = fs.createReadStream(inputFile);
var out = fs.createWriteStream(temp);

console.log('decompressing');
var unzip = inp.pipe(gzip).pipe(out); // unpack into catenated file

unzip.on('finish', function unzipCallback(){
    if (inp.end) inp.end()
    if (out.end) out.end();

    console.log('unpacking files');
    unpack(temp, outputPath);
    console.log('done');
});

// then run this over the structure...
// ToDo: see if this can be re-written as a single pipeline

function unpack(srcPack, targetPath) {
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

    while (remains > 0){
        var next = (remains > buffer.length) ? (buffer.length) : (remains);
        var rlen = fs.readSync(fd, buffer, 0, next, null);
        if (rlen < 1) throw new Error('Malformed file: truncated file data');
        remains -= rlen;

        var data = buffer.slice(0, rlen);
        for (var i = 0; i < paths.length; i++){
            var npath = target + paths[i];
            ensureDirectory(path.dirname(npath));
            fs.appendFileSync(npath, data);
        }
    }
}

// Helper to process two streams of work with callback IO
//function ContinuationWorker(
//    sourcePump/*function()=> data*/,
//    targetPump/*function()=> [data]*/,
//    fullyComplete/*callback for when source pump returns nothing*/,
//    doWork/*function(sourceData, targetData, nextCallback)*/
//    )/*:void*/ {
//    var sourceItem = null;
//    var targetItems = [];
//    var output = []
//
//    var trampoline = function trampoline(err, result) {
//        if (err) { // failed a step. All results considered unreliable.
//            return fullyComplete(err, undefined);
//        }
//
//        if (result) { // got something from the worker function
//            output.push(result);
//        }
//
//        if (sourceItem == null || sourceItem == undefined) { // work complete
//            return fullyComplete(null, output);
//        }
//
//        if (targetItems.length < 1) { // pump the source and targets then start again
//            sourceItem = sourcePump();
//            targetItems = targetPump();
//            return trampoline(null, null);
//        }
//
//        // do next work item
//        var targetItem = targetItems.pop();
//        return doWork(sourceItem, targetItem, trampoline.bind(this));
//    };
//    trampoline(null, null);
//}

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

            // In the case of any other error, just see if there's a dir
            // there already.  If so, then hooray!  If not, then something
            // is borked.
            default:
                var stat;
                try {
                    stat = fs.statSync(p);
                } catch (err1) {
                    throw err0;
                }
                if (!stat.isDirectory()) throw err0;
                break;
        }
    }
};

//fs.read(fd, buffer, offset, length, position, function(err, bytesRead, buffer){});
