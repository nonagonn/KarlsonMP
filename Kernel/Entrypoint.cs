using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Doorstop
{
    public class Entrypoint
    {
        public static void Start()
        {
            new Thread(() =>
            {
                while (AppDomain.CurrentDomain.GetAssemblies().Count(x => x.GetName().Name == "Assembly-CSharp") == 0) { }
                while (AppDomain.CurrentDomain.GetAssemblies().Count(x => x.GetName().Name == "UnityEngine") == 0) { }
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;

                var preloader = Assembly.LoadFrom(Path.Combine(Directory.GetCurrentDirectory(), "KMP", "Preloader.dll"));
                preloader.GetType("Preloader.Entrypoint").GetMethod("Start").Invoke(null, Array.Empty<object>());
            }).Start();

            
        }

        static Dictionary<string, Assembly> loadedAssemblies = new Dictionary<string, Assembly>();

        private static void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            loadedAssemblies[args.LoadedAssembly.FullName] = args.LoadedAssembly;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if(loadedAssemblies.ContainsKey(args.Name))
                return loadedAssemblies[args.Name];
            var resolved = Path.Combine(Directory.GetCurrentDirectory(), "KarlsonMP", new AssemblyName(args.Name).Name + ".dll");
            if(File.Exists(resolved)) return Assembly.LoadFrom(resolved);
            return null;
        }
    }
}
