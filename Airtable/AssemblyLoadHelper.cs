namespace Airtable
{
    public static class AssemblyLoadHelper
    {
        //[MethodImpl(MethodImplOptions.NoInlining)]
        //public static void ExecuteAndUnload(string assemblyPath, out WeakReference alcWeakRef)
        //{
        //    // Create the unloadable HostAssemblyLoadContext
        //    var alc = new HostAssemblyLoadContext(assemblyPath);

        //    // Create a weak reference to the AssemblyLoadContext that will allow us to detect
        //    // when the unload completes.
        //    alcWeakRef = new WeakReference(alc);

        //    // Load the plugin assembly into the HostAssemblyLoadContext. 
        //    // NOTE: the assemblyPath must be an absolute path.
        //    Assembly a = alc.LoadFromAssemblyPath(assemblyPath);

        //    // Get the plugin interface by calling the PluginClass.GetInterface method via reflection.
        //    Type pluginType = a.GetType("Plugin.PluginClass");
        //    MethodInfo getInterface = pluginType.GetMethod("GetInterface", BindingFlags.Static | BindingFlags.Public);
        //    Plugin.Interface plugin = (Plugin.Interface)getInterface.Invoke(null, null);

        //    // This initiates the unload of the HostAssemblyLoadContext. The actual unloading doesn't happen
        //    // right away, GC has to kick in later to collect all the stuff.
        //    alc.Unload();
        //}
    }
}
