/*
* Copyright 2020-2022 Lava Gang
* as per MelonLoader's LICENSE
* 
* This file has been sampled from
* https://github.com/LavaGang/MelonLoader/blob/master/MelonLoader/Fixes/ForcedCultureInfo.cs
* 
* This part of code is strictly licensed under MelonLoader's license, Apache-2.0 license
* https://github.com/LavaGang/MelonLoader/blob/master/LICENSE.md
*/

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Preloader
{
    public static class ForcedCultureInfo
    {
        private static CultureInfo SelectedCultureInfo = CultureInfo.InvariantCulture;

        public static void Install()
        {
            Type PatchType = typeof(ForcedCultureInfo);
            HarmonyMethod PatchMethod_Get = new HarmonyMethod(PatchType.GetMethod("GetMethod", BindingFlags.NonPublic | BindingFlags.Static));
            HarmonyMethod PatchMethod_Set = new HarmonyMethod(PatchType.GetMethod("SetMethod", BindingFlags.NonPublic | BindingFlags.Static));

            Type CultureInfoType = typeof(CultureInfo);
            Type ThreadType = typeof(Thread);

            foreach (FieldInfo fieldInfo in ThreadType
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(x => x.FieldType == CultureInfoType))
                fieldInfo.SetValue(null, SelectedCultureInfo);

            foreach (PropertyInfo propertyInfo in ThreadType
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Where(x => x.PropertyType == CultureInfoType))
            {
                MethodInfo getMethod = propertyInfo.GetGetMethod();
                if (getMethod != null)
                    try { Entrypoint.harmony.Patch(getMethod, PatchMethod_Get); }
                    catch { }

                MethodInfo setMethod = propertyInfo.GetSetMethod();
                if (setMethod != null)
                    try { Entrypoint.harmony.Patch(setMethod, PatchMethod_Set); }
                    catch { }
            }
        }

        private static bool GetMethod(ref CultureInfo __result)
        {
            __result = SelectedCultureInfo;
            return false;
        }
        private static bool SetMethod() => false;
    }
}