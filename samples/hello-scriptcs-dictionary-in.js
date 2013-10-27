var edge = require('edge');
var hello = edge.func('scs', function() {/*
static object Invoke(IDictionary<string,object> data) {
    Console.WriteLine("-----> In .NET:");
    foreach (var kv in data)
    {
        Console.WriteLine(kv.Key + " : " + kv.Value );
    }

    return null;
}
*/});

var payload = {
        anInteger: 1,
        aNumber: 3.1415,
        aString: 'foobar',
        aBool: true,
        anObject: {},
        anArray: [ 'a', 1, true ],
        aBuffer: new Buffer(1024)
}

hello(payload, function(error,result) {
    if (error) throw error;
    console.log(result);
}); 



