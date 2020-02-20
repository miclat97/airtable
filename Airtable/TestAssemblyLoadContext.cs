using Plugin;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

class TestAssemblyLoadContext : AssemblyLoadContext
{
    public TestAssemblyLoadContext() : base(isCollectible: true)
    {
    }

    protected override Assembly Load(AssemblyName name)
    {
        return null;
    }

    static Interface LoadAssemblyAsFileStream(string filename)
    {
        Assembly classLibrary1 = null;
        using (FileStream fs = File.Open(filename, FileMode.Open))
        {
            using (MemoryStream ms = new MemoryStream())
            {
                byte[] buffer = new byte[1024];
                int read = 0;
                while ((read = fs.Read(buffer, 0, 1024)) > 0)
                    ms.Write(buffer, 0, read);
                classLibrary1 = Assembly.Load(ms.ToArray());
            }
        }
        foreach (Type type in classLibrary1.GetExportedTypes())
        {
            if (type.GetInterface("Interface") != null)
                return Activator.CreateInstance(type) as PluginClass;
        }

        throw new Exception("no class found that implements interface IClass1");
    }
}
