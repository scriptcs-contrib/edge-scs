var edge = require('edge');
var hello = edge.func('scs', function() {/*
static Task<object> Invoke(string s) {
    return Task.FromResult<object>(s);
}
*/});
hello("Hello from scriptcs", function(error,result) {
    if (error) throw error;
    console.log(result);
}); 
