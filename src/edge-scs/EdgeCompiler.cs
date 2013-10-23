using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using ScriptCs;
using ScriptCs.Contracts;
using ScriptCs.Exceptions;
using System.Threading.Tasks;

//based on EdgeCompiler code in edge-cs: https://github.com/tjanczuk/edge-cs/blob/master/src/edge-cs/EdgeCompiler.cs
public class EdgeCompiler
{
    static readonly bool debuggingEnabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("EDGE_CS_DEBUG"));
    static readonly bool debuggingSelfEnabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("EDGE_CS_DEBUG_SELF"));
    static Dictionary<string, Assembly> referencedAssemblies = new Dictionary<string, Assembly>();
    
    static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
        var name = new AssemblyName(args.Name).Name;
        Assembly assembly = null;
        var found = referencedAssemblies.TryGetValue(name, out assembly);
        return assembly;
    }

    static EdgeCompiler()
    {
        //add System.Core
        referencedAssemblies.Add(typeof(Enumerable).Assembly.GetName().Name, typeof(Enumerable).Assembly);

        //add System
        referencedAssemblies.Add(typeof(Uri).Assembly.GetName().Name, typeof(Uri).Assembly);

        var edgeAssembly = Assembly.GetExecutingAssembly();
        var bin = Path.GetDirectoryName(edgeAssembly.Location);

        foreach (var file in Directory.GetFiles(bin, "*.dll"))
        {
            var assembly = Assembly.LoadFile(file);

            if (!referencedAssemblies.ContainsKey(assembly.FullName))
                referencedAssemblies.Add(assembly.GetName().Name, assembly);
        }

        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

    }

    public EdgeCompiler()
    {
    }

    public Func<object, Task<object>> CompileFunc(IDictionary<string, object> parameters)
    {
        string source = (string)parameters["source"];
        string lineDirective = string.Empty;
        string fileName = null;
        Func<object, Task<object>> invoker = null; 

        // read source from file
        if (source.EndsWith(".csx", StringComparison.InvariantCultureIgnoreCase))
        {
            // retain fileName for debugging purposes
            if (debuggingEnabled)
            {
                fileName = source;
            }

            source = File.ReadAllText(source);
        }

        // add assembly references provided explicitly through parameters
        List<string> references = new List<string>();
        object v;
        if (parameters.TryGetValue("references", out v))
        {
            foreach (object reference in (object[])v)
            {
                references.Add((string)reference);
            }
        }

        // try to compile source code as a class library
        string errors;
            
        if (!this.TryCompile(source, references, out errors, out invoker))
        {
            throw new InvalidOperationException(
                "Unable to compile C# code.\n----> Errors when compiling as a CLR library:\n"
                + errors);
        }

        // store referenced assemblies to help resolve them at runtime from AppDomain.AssemblyResolve

        if (invoker == null)
        {
            throw new InvalidOperationException("Script did not return a Func<object, Task<object>>");
        }
        return invoker;
    }

    bool TryCompile(string source, List<string> references, out string errors, out Func<object, Task<object>> invoker)
    {
        invoker = null;
        errors = null;
        var console = new ScriptConsole();
        var loggerConfig=new LoggerConfigurator(ScriptCs.Contracts.LogLevel.Error);
        loggerConfig.Configure(console);
        var logger = loggerConfig.GetLogger();

        var builder = new ScriptServicesBuilder(console, logger).
            InMemory(true).
            Repl(false);
        
        var services = builder.Build();
        var executor = services.Executor;
        executor.Initialize(Enumerable.Empty<string>(), Enumerable.Empty<IScriptPack>());
        if (source.ToLower().EndsWith(".csx"))
            source = "#load " + Environment.NewLine + source;

        executor.AddReferences(references.ToArray());
        var result = executor.ExecuteScript(source);
        if (result.CompileExceptionInfo != null)
        {
            var scriptException = (ScriptCompilationException)result.CompileExceptionInfo.SourceException;
            errors = scriptException.Message;
            return false;
        }
        var returnValue = result.ReturnValue;
        var method = (Delegate) returnValue;

        invoker = new Func<object, Task<object>>(v=> {
            return Task.FromResult<object>(method.DynamicInvoke(new[] {v}));
        });

        errors = null;
        return true;
    }
}
