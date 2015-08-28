// Node version of the .Net simple compress app

var fs = require('fs');
var zlib = require('zlib');

// to start with, unpack a presupplied filesystem:
var inputFile = 'C:/Temp/sample.inpkg';
var temp = inputFile + '.tmp';

// Unzip archive to temp location
if (fs.existsSync(temp)) {fs.truncateSync(temp, 0);}
var gzip = zlib.createGunzip();
var inp = fs.createReadStream(inputFile);
var out = fs.createWriteStream(temp);

inp.pipe(gzip).pipe(out); // unpack into catenated file

inp.end();
out.end();

// then run this over the structure...
fs.read(fd, buffer, offset, length, position, function(err, bytesRead, buffer){});
