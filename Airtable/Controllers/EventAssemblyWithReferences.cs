using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Airtable.Controllers
{
    public class EventAssemblyWithReferences
    {
        public static CSharpCompilation _cSharpCompilation;

        static EventAssemblyWithReferences()
        {
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
                        MetadataReference.CreateFromFile(Path.Combine(dotnetCoreDirectory, "System.Collections.dll")),
                        MetadataReference.CreateFromFile(Path.Combine(dotnetCoreDirectory, "System.Runtime.dll")));
        }
    }
}
