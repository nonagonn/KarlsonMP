using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kernel
{
    public class Kernel
    {
        public static string LOADSON_ROOT;
        private static Assembly loadson;

        public static void Inject()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            
            loadson = Assembly.LoadFrom(Path.Combine(Directory.GetCurrentDirectory(), "KMP", "KarlsonMP.dll"));
            loadson.GetType("KarlsonMP.Loader").GetMethod("Start").Invoke(null, Array.Empty<object>());
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (new AssemblyName(args.Name).Name == "KarlsonMP")
                return loadson;
            string resolved = Path.Combine(Directory.GetCurrentDirectory(), "KMP", new AssemblyName(args.Name).Name + ".dll");
            if(File.Exists(resolved)) return Assembly.LoadFrom(resolved);
            return null;
        }
    }
}
