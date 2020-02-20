using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Plugin;

namespace Airtable.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        private CSharpCompilation _cSharpCompilation;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;

            var dotnetCoreDirectory = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();

            _cSharpCompilation = CSharpCompilation.Create("AssemblyName")
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(
                    MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Console).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(Path.Combine("C:\\dotnet", "MongoDB.Driver.dll")),
                    MetadataReference.CreateFromFile(Path.Combine("C:\\dotnet", "MongoDB.Driver.Core.dll")),
                    MetadataReference.CreateFromFile(Path.Combine("C:\\dotnet", "MongoDB.Bson.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(dotnetCoreDirectory, "mscorlib.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(dotnetCoreDirectory, "netstandard.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(dotnetCoreDirectory, "System.Linq.Expressions.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(dotnetCoreDirectory, "System.Threading.Tasks.dll")),
                    MetadataReference.CreateFromFile(Path.Combine(dotnetCoreDirectory, "System.Runtime.dll")));
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpPost("js/{firstNumber}/{secondNumber}")]
        public async Task<IActionResult> ExecuteJavaScriptCode([FromServices] INodeServices nodeServices, [FromRoute] string firstNumber, [FromRoute] string secondNumber)
        {
            string jsCode = "";
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                jsCode = await reader.ReadToEndAsync();
            }


            string tempJsFileName = @"tempJsFile.js";


            using (FileStream file = new FileStream(tempJsFileName, FileMode.Create, System.IO.FileAccess.Write))
            {
                await file.WriteAsync(Encoding.UTF8.GetBytes(jsCode));
            }

            var add = await nodeServices.InvokeExportAsync<int>($"./{tempJsFileName}", "add", int.Parse(firstNumber), int.Parse(secondNumber));

            var fileInfo = new FileInfo(tempJsFileName);
            fileInfo.Delete();

            return Ok(add);
        }

        [HttpPost("jsString/{functionName}/{firstNumber}/{secondNumber}")]
        public async Task<IActionResult> ExecuteJavaScriptCodeString([FromServices] INodeServices nodeServices, [FromRoute] string functionName, [FromRoute] string firstNumber, [FromRoute] string secondNumber)
        {
            string jsCode = "";
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                jsCode = await reader.ReadToEndAsync();
            }

            using (StringAsTempFile temp = new StringAsTempFile(jsCode, CancellationToken.None))
            {
                try
                {
                    return Ok(await nodeServices.InvokeAsync<object>(temp.FileName, functionName, int.Parse(firstNumber), int.Parse(secondNumber)));
                }
                catch (Exception ex)
                {
                    return StatusCode(415, ex.StackTrace);
                }
            }
        }

        [HttpPost("jsConnect/{functionName}")]
        public async Task<IActionResult> ExecuteJavaScriptWithConnectionToMongoDb([FromServices] INodeServices nodeServices, [FromRoute] string functionName)
        {
            string sendedJsCode = "";
            string createMongoClientLine = "var MongoClient = require('mongodb').MongoClient;";
            string connectionStringLine = "var url = \"mongodb://localhost:27017/\";";
            string appendToFunctionLine = "module.exports.MongoClient.connect(url, ";
            string endingLine = "});";

            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                sendedJsCode = await reader.ReadToEndAsync();
            }

            StringBuilder codeToExecuteBuilder = new StringBuilder(createMongoClientLine);
            codeToExecuteBuilder.Append(Environment.NewLine);
            codeToExecuteBuilder.Append(connectionStringLine);
            codeToExecuteBuilder.Append(Environment.NewLine);
            codeToExecuteBuilder.Append(appendToFunctionLine);
            codeToExecuteBuilder.Append(sendedJsCode);
            codeToExecuteBuilder.Append(Environment.NewLine);
            codeToExecuteBuilder.Append(endingLine);

            string jsToExecute = codeToExecuteBuilder.ToString();

            object result;

            using (StringAsTempFile temp = new StringAsTempFile(jsToExecute, CancellationToken.None))
            {
                result = await nodeServices.InvokeExportAsync<object>(temp.FileName, functionName);
            }

            return Ok(result);
        }

        [HttpGet("mongojs")]
        public async Task<IActionResult> MongoJs([FromServices] INodeServices nodeServices)
        {
            await nodeServices.InvokeAsync<object>($"./node.js", "connect");
            return Ok();
        }

        [HttpGet("mongodb")]
        public IActionResult MongoDb()
        {
            var client = new MongoClient("mongodb://localhost:27017");

            var newInsert = new BsonDocument();

            newInsert.Add("test1", "as");

            client.GetDatabase("test").GetCollection<BsonDocument>("test").InsertOne(newInsert);

            return Ok(client.GetDatabase("test").GetCollection<BsonDocument>("test").Find(new BsonDocument()).ToJson());
        }

        //[HttpGet("mongodb/{val1}/{val2}")]
        //public IActionResult MongoDbValues([FromRoute] string val1, [FromRoute] string val2)
        //{
        //    var client = new MongoClient("mongodb://localhost:27017");

        //    var newInsert = new BsonDocument().Add(val1, val2);

        //    client.GetDatabase("test").GetCollection<BsonDocument>("test").InsertOne(newInsert);

        //    return Ok($"Value inserted: {val1} : {val2}");
        //}

        [HttpPost("csharp")]
        public async Task<IActionResult> Csharp()
        {
            var dotnetCoreDirectory = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();

            //CSharpCompilation.CreateScriptCompilation("test", CSharpSyntaxTree.ParseText("tst"), )

            //var codeToExecuteAtMongoDV = @"                                    
            //                   var newInsert = new BsonDocument();
            //                   client.GetDatabase(""test"").GetCollection<BsonDocument>(""test2"").InsertOne(newInsert);
            //";

            _cSharpCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(
                            @"using MongoDB.Driver;
                                    using MongoDB.Bson;
                                    using System.Linq;

                                    public static class ClassName 
                                    { 
                                        public static void MethodName()
                                        {
                                            var client = new MongoClient(""mongodb://localhost:27017"");


                                            var newInsert = new BsonDocument();

                                            newInsert.Add(""test1"", ""as"");

                                            client.GetDatabase(""test"").GetCollection<BsonDocument>(""test2"").InsertOne(newInsert);
                                        }
                                    }"));

            //var dotnetCoreDirectory = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();

            //var compilation = CSharpCompilation.Create("AssemblyName")
            //    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            //    .AddReferences(
            //        MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
            //        MetadataReference.CreateFromFile(typeof(Console).GetTypeInfo().Assembly.Location),
            //        MetadataReference.CreateFromFile(Path.Combine("C:\\dotnet", "MongoDB.Driver.dll")),
            //        MetadataReference.CreateFromFile(Path.Combine("C:\\dotnet", "MongoDB.Driver.Core.dll")),
            //        MetadataReference.CreateFromFile(Path.Combine("C:\\dotnet", "MongoDB.Bson.dll")),
            //        MetadataReference.CreateFromFile(Path.Combine(dotnetCoreDirectory, "mscorlib.dll")),
            //        MetadataReference.CreateFromFile(Path.Combine(dotnetCoreDirectory, "netstandard.dll")),
            //        MetadataReference.CreateFromFile(Path.Combine(dotnetCoreDirectory, "System.Linq.Expressions.dll")),
            //        MetadataReference.CreateFromFile(Path.Combine(dotnetCoreDirectory, "System.Threading.Tasks.dll")),
            //        MetadataReference.CreateFromFile(Path.Combine(dotnetCoreDirectory, "System.Collections.dll")),
            //        MetadataReference.CreateFromFile(Path.Combine(dotnetCoreDirectory, "System.Runtime.dll")))
            //            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(
            //                @"using MongoDB.Driver;
            //                        using MongoDB.Bson;
            //                        using System.Linq;

            //                        public static class ClassName 
            //                        { 
            //                            public static void MethodName()
            //                            {
            //                                var client = new MongoClient(""mongodb://localhost:27017"");


            //                                var newInsert = new BsonDocument();

            //                                newInsert.Add(""test1"", ""as"");

            //                                client.GetDatabase(""test"").GetCollection<BsonDocument>(""test2"").InsertOne(newInsert);
            //                            }
            //                        }"));

            //string lastCompilerMessage = "";

            //foreach (var compilerMessage in compilation.GetDiagnostics())
            //{
            //    if (compilerMessage.WarningLevel == 0)
            //    {
            //        lastCompilerMessage = compilerMessage.ToString();
            //    }
            //    Console.WriteLine(compilerMessage);
            //}


            using (var memoryStream = new MemoryStream())
            {
                var emitResult = _cSharpCompilation.Emit(memoryStream);
                if (emitResult.Success)
                {
                    //var context = AssemblyLoadContext.Default;
                    //var assembly = context.LoadFromStream(memoryStream);

                    var assembly = Assembly.Load(memoryStream.ToArray());

                    assembly.GetType("ClassName").GetMethod("MethodName").Invoke(null, null);
                }
                else
                {
                    return StatusCode(500, "Error compilation");
                }
            }

            GC.Collect();
            return Ok();
        }

        //        [HttpGet("assembliesFromWorkingDirectory")]
        //        public IActionResult AssembliesFromWorkingDirectory()
        //        {
        //            var assemblies = new HashSet<Assembly>();

        //            ReferencesHelper.Collect(assemblies, Assembly.Load(new AssemblyName("netstandard")));

        //            string currentAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        //#if DEBUG
        //            string configName = "Debug";
        //#else
        //                            string configName = "Release";
        //#endif
        //            WeakReference hostAlcWeakRef;

        //            string pluginFullPath = Path.Combine(currentAssemblyDirectory, $"..\\..\\..\\..\\Plugin\\bin\\{configName}\\netcoreapp3.1\\Plugin.dll");
        //            Program.ExecuteAndUnload(pluginFullPath, out hostAlcWeakRef);

        //            var resolver = new AssemblyDependencyResolver(pluginPath);

        //            string assemblyPath = HostAssemblyLoadContext._resolver.ResolveAssemblyToPath(name);
        //            if (assemblyPath != null)
        //            {
        //                Console.WriteLine($"Loading assembly {assemblyPath} into the HostAssemblyLoadContext");
        //                return LoadFromAssemblyPath(assemblyPath);
        //            }

        //            var alc = new HostAssemblyLoadContext(assemblyPath);

        //            // Create a weak reference to the AssemblyLoadContext that will allow us to detect
        //            // when the unload completes.
        //            alcWeakRef = new WeakReference(alc);

        //            //// add extra assemblies which are not part of netstandard.dll, for example:
        //            //Collect(typeof(Uri).Assembly);

        //            // second, build metadata references for these assemblies
        //            var result = new List<MetadataReference>(assemblies.Count);
        //            foreach (var assembly in assemblies)
        //            {
        //                result.Add(MetadataReference.CreateFromFile(assembly.Location));
        //            }


        //            List<string> usingReferences = new List<string>();

        //            usingReferences.Add("MongoDB.Driver");
        //            usingReferences.Add("")

        //                            var compilation = CSharpCompilation.Create("AssemblyName")
        //                                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, usings: ))
        //                                .AddReferences(result)
        //                                        .AddSyntaxTrees(CSharpSyntaxTree.ParseText(
        //                                            @"using MongoDB.Driver;
        //                                            using MongoDB.Bson;
        //                                            using System.Linq;

        //                                            public static class ClassName 
        //                                            { 
        //                                                public static void MethodName() 
        //                                                {
        //                                                    var client = new MongoClient(""mongodb://localhost:27017"");


        //                                                    var result = client.GetDatabase(""test"").GetCollection<BsonDocument>(""test1"").Find(new BsonDocument()).ToJson();
        //                                                    System.Console.WriteLine(result);
        //                                                }
        //                                            }"));

        //            string lastCompilerMessage = "";

        //            foreach (var compilerMessage in compilation.GetDiagnostics())
        //            {
        //                if (compilerMessage.WarningLevel == 0)
        //                {
        //                    lastCompilerMessage = compilerMessage.ToString();
        //                }
        //                Console.WriteLine(compilerMessage);
        //            }


        //            using (var memoryStream = new MemoryStream())
        //            {
        //                var emitResult = compilation.Emit(memoryStream);
        //                if (emitResult.Success)
        //                {
        //                    //var context = AssemblyLoadContext.Default;
        //                    //var assembly = context.LoadFromStream(memoryStream);

        //                    var assembly = Assembly.Load(memoryStream.ToArray());

        //                    assembly.GetType("ClassName").GetMethod("MethodName").Invoke(null, null);
        //                }
        //                else
        //                {
        //                    return StatusCode(500, lastCompilerMessage);
        //                }
        //            }

        //            return Ok();
        //        }

        //        [HttpGet("LoadAssemblyAsDLLIntoMemoryAndDeleteIt")]
        //        public IActionResult LoadAssemblyAsDLLIntoMemoryAndDeleteIt()
        //        {
        //            IOtherGenericInterfaceToLoadAssemblyIntoMemory testInterface =
        //        }
    }
}