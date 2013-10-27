using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using NuGet;
using ScriptCs;
using ScriptCs.Contracts;
using ScriptCs.Engine.Roslyn;
using ScriptCs.Exceptions;
using System.Threading.Tasks;

//based on EdgeCompiler code in edge-cs: https://github.com/tjanczuk/edge-cs/blob/master/src/edge-cs/EdgeCompiler.cs
public class EdgeCompiler
{
    private static readonly bool debuggingEnabled =
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("EDGE_CS_DEBUG"));

    private static Dictionary<string, Assembly> referencedAssemblies = new Dictionary<string, Assembly>();

    private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    { 
        var name = new AssemblyName(args.Name).Name;
        Assembly assembly = null;
        var found = referencedAssemblies.TryGetValue(name, out assembly);
        return assembly;
    }

    static EdgeCompiler()
    {
        //add System.Core
        referencedAssemblies.Add(typeof (Enumerable).Assembly.GetName().Name, typeof (Enumerable).Assembly);

        //add System
        referencedAssemblies.Add(typeof (Uri).Assembly.GetName().Name, typeof (Uri).Assembly);

        var edgeAssembly = Assembly.GetExecutingAssembly();
        var bin = Path.GetDirectoryName(edgeAssembly.Location);

        //populate the assembly cache
        foreach (var file in Directory.GetFiles(bin, "*.dll"))
        {
            var assembly = Assembly.LoadFile(file);

            if (!referencedAssemblies.ContainsKey(assembly.FullName))
                referencedAssemblies.Add(assembly.GetName().Name, assembly);
        }

        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

    }

    public Func<object, Task<object>> CompileFunc(IDictionary<string, object> parameters)
    {
        string source = (string) parameters["source"];
        string lineDirective = string.Empty;
        Func<object, Task<object>> invoker = null;

        // read source from file
        if (source.EndsWith(".csx", StringComparison.InvariantCultureIgnoreCase))
        {
            source = File.ReadAllText(source);
        }

        // add assembly references provided explicitly through parameters
        var references = GetParamReferences(parameters);

        // try to compile source code as a class library
        string errors;

        var executor = GetExecutor(references);

        if (!this.TryCompile(source, executor, out errors, out invoker))
        {
            throw new InvalidOperationException(
                "Unable to compile C# code.\n----> Errors when compiling as a CLR library:\n"
                + errors);
        }

        // store referenced assemblies to help resolve them at runtime from AppDomain.AssemblyResolve

        return invoker;
    }

    private static List<string> GetParamReferences(IDictionary<string, object> parameters)
    {
        List<string> references = new List<string>();
        object v;

        if (parameters.TryGetValue("references", out v))
        {
            foreach (object reference in (object[]) v)
            {
                references.Add((string) reference);
            }
        }
        return references;
    }

    private IScriptExecutor GetExecutor(List<string> references)
    {
        var console = new ScriptConsole();
        var loggerConfig = new LoggerConfigurator(ScriptCs.Contracts.LogLevel.Error);
        loggerConfig.Configure(console);
        var logger = loggerConfig.GetLogger();

        var builder = new ScriptServicesBuilder(console, logger).
            InMemory(true);

        var services = builder.Build();
        var executor = services.Executor;
        executor.Initialize(Enumerable.Empty<string>(), Enumerable.Empty<IScriptPack>());

        executor.AddReferences(references.ToArray());
        var reference = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\ScriptCs.Contracts.dll";
        executor.AddReferences(reference);
        executor.ImportNamespaces("ScriptCs.Contracts");
        return executor;
    }

    private bool TryCompile(string source, IScriptExecutor executor, out string errors,
        out Func<object, Task<object>> invoker)
    {
        invoker = null;
        errors = null;
 
        if (source.ToLower().EndsWith(".csx"))
            source = "#load " + Environment.NewLine + source;

        //return the type 
        source += "System.Reflection.MethodInfo.GetCurrentMethod().DeclaringType";

        var result = executor.ExecuteScript(source);
        if (result.CompileExceptionInfo != null)
        {
            var scriptException = (ScriptCompilationException) result.CompileExceptionInfo.SourceException;
            errors = scriptException.Message;
            return false;
        }
        var returnValue = result.ReturnValue;
        var type = (Type) returnValue;

        invoker = GetInvoker(type);

        errors = null;
        return true;
    }

    private Func<object, Task<object>> GetInvoker(Type type)
    {
        Func<object, Task<object>> invoker;
        var invokeMethod = type.GetMethod("Invoke");

        if (invokeMethod == null)
        {
            throw new InvalidOperationException("Script does not have a static Invoke method");
        }

        if (invokeMethod.ReturnType != typeof (Task<object>))
        {
            invoker = p =>
            {
                var task = new Task<object>(() => invokeMethod.Invoke(null, new[] {p}));
                task.Start();
                return task;
            };
        }
        else
            invoker = p => (Task<object>) invokeMethod.Invoke(null, new[] {p});

        return invoker;
    }
}

