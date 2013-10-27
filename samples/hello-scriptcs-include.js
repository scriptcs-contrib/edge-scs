var edge = require('edge');
var hello = edge.func('scs', "hello-scriptcs-include.csx");
hello("Hello from scriptcs", function(error,result) {
    if (error) throw error;
    console.log(result);
}); 