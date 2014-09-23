var MSBuild = require('msbuild');

var msbuild = MSBuild(function(err){
	if (err) console.log(err);
});

msbuild.sourcePath = "src\\edge-scs\\edge-scs.sln";
msbuild.configuration = "Release";
msbuild.publishProfile = "";

msbuild.build();