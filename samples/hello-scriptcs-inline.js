var edge = require('edge');
var hello = edge.func('scs', function() {/*
Func<IDictionary<string,object>, Task<object>> execute = p=> Task.FromResult<object>(p["value"]); 
execute
*/});
hello({"value": "Hello from scriptcs"}, function(error,result) {
    if (error) throw error;
    console.log(result.Result);
}); 
