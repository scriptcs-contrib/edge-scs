var version = "0.10.2"

var request = require('request');
var unzip = require('unzip');
var fs = require('fs');
var path = require('path');

var getScriptCs = function() {
	var response = request("http://chocolatey.org/api/v2/package/ScriptCs/" + version);

	response.pipe(unzip.Extract({path:"scs"}))
	.on("end", function(){
		copyDir(path.join("scs","tools","scriptcs"), "lib");
	});
}

var copyDir = function(src, dest) {
	var files = fs.readdirSync(src);
	for(var i = 0; i < files.length; i++) {
		var current = fs.lstatSync(path.join(src, files[i]));
		if(current.isDirectory()) {
			copyDir(path.join(src, files[i]), path.join(dest, files[i]));
		} else if(current.isSymbolicLink()) {
			var symlink = fs.readlinkSync(path.join(src, files[i]));
			fs.symlinkSync(symlink, path.join(dest, files[i]));
		} else {
			copy(path.join(src, files[i]), path.join(dest, files[i]));
		}
	}
};
 
var copy = function(src, dest) {
	fs.createReadStream(src).pipe(fs.createWriteStream(dest));
};

getScriptCs();
