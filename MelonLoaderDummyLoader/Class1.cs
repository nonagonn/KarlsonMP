using MelonLoader;
using MelonLoaderDummyLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

[assembly: MelonInfo(typeof(MLDummyLoader), "KMP_MLDummyLoader", "1.0.0", "devilExE")]
[assembly: MelonGame("Dani", "Karlson")]
namespace MelonLoaderDummyLoader
{

    public class MLDummyLoader : MelonMod
    {
        public override void OnLateInitializeMelon()
        {
            var kernel = Assembly.LoadFrom(Path.Combine(Directory.GetCurrentDirectory(), "KMP", "Kernel.dll"));
            kernel.GetType("Kernel.Kernel").GetMethod("Inject").Invoke(null, Array.Empty<object>());
            var unity = (from x in AppDomain.CurrentDomain.GetAssemblies() where x.GetName().Name == "UnityEngine.CoreModule" select x).First();
            unity.GetType("UnityEngine.SceneManagement.SceneManager").GetMethod("LoadScene", new Type[] { typeof(int) }).Invoke(null, new object[] { (int)0 });
        }
    }
}
