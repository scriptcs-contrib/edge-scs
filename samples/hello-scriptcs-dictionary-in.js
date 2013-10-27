var edge = require('edge');
var hello = edge.func('scs', function() {/*
static object Invoke(IDictionary<string,object> d) {
    return d["value"];
}
*/});
hello({"value": "Hello from scriptcs"}, function(error,result) {
    if (error) throw error;
    console.log(result);
}); 
