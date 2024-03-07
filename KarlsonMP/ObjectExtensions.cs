using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KarlsonMP
{
    public static class ObjectExtensions
    {
        public static T ReflectionGet<T>(this object obj, string FieldName)
        {
            return (T)obj.GetType().GetField(FieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(obj);
        }
        public static void ReflectionSet<T>(this object obj, string FieldName, T value)
        {
            obj.GetType().GetField(FieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(obj, value);
        }

        public static void ReflectionInvoke(this object obj, string MethodName) => ReflectionInvoke(obj, MethodName, Array.Empty<object>());
        public static void ReflectionInvoke(this object obj, string MethodName, params object[] Args)
        {
            obj.GetType().GetMethod(MethodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(obj, Args);
        }
        public static T ReflectionInvoke<T>(this object obj, string MethodName) => ReflectionInvoke<T>(obj, MethodName, Array.Empty<object>());
        public static T ReflectionInvoke<T>(this object obj, string MethodName, params object[] Args)
        {
            return (T)obj.GetType().GetMethod(MethodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(obj, Args);
        }
    }
}
