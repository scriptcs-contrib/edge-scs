var edge = require('edge');
var hello = edge.func('scs', function() {/*
static object Invoke(string s) {
    return new {message=s};
}
*/});
hello("Hello from scriptcs", function(error,result) {
    if (error) throw error;
    console.log(result);
}); 
