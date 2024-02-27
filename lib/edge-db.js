const os = require('os');

const platform = os.platform();

exports.getCompiler= function (){
	console.log("giving getCompiler");
	return process.env.EDGE_SQL_NATIVE || (require('path').join(__dirname, platform, 'edge-db.dll'));
}

exports.getBootstrapDependencyManifest= function (){
	console.log("giving getBootstrapDependencyManifest");
	return process.env.EDGE_SQL_NATIVE || (require('path').join(__dirname, platform, 'edge-db.deps.json'))
}

/**


 **/