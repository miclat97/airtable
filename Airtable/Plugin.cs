using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Plugin
{
    public class PluginClass : Interface
    {
        private static void OnPluginUnloadingRequested(AssemblyLoadContext obj) { var assembly = obj.Assemblies.FirstOrDefault(x => x.FullName.Contains("Microsoft.Data.SqlClient", StringComparison.InvariantCultureIgnoreCase)); var dispatcher = assembly.GetType("Microsoft.Data.SqlClient.SqlDependencyPerAppDomainDispatcher"); var value = dispatcher.GetField("SingletonInstance", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null); var method = dispatcher.GetMethod("UnloadEventHandler", BindingFlags.NonPublic | BindingFlags.Instance); method.Invoke(value, new object[] { null, null }); }


        public static Interface GetInterface()
        {
            PluginClass plugin = new PluginClass();

            // We register handler for the Unloading event of the context that we are running in 
            // so that we can perform cleanup of stuff that would otherwise prevent unloading
            // (Like freeing GCHandles for objects of types loaded into the unloadable AssemblyLoadContext,
            // terminating threads running code in assemblies loaded into the unloadable AssemblyLoadContext,
            // etc.)
            // NOTE: this is optional and likely not required for basic scenarios
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            AssemblyLoadContext currentContext = AssemblyLoadContext.GetLoadContext(currentAssembly);
            currentContext.Unloading += OnPluginUnloadingRequested;

            return plugin;
        }
    }
}
