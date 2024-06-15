using Belzont.Interfaces;
using Colossal.OdinSerializer.Utilities;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Belzont.Utils
{
    public static class BridgeUtils
    {
        public static object[] GetAllLoadableClassesInAssemblyList(Type t, IEnumerable<Assembly> assemblies = null)
        {
            assemblies ??= AppDomain.CurrentDomain.GetAssemblies();
            return assemblies.SelectMany(s =>
            {
                try
                {
                    return s.GetExportedTypes();
                }
                catch (ReflectionTypeLoadException tle)
                {
                    return tle.Types;
                }
                catch (Exception e)
                {
                    LogUtils.DoWarnLog($"Error exporting types from assembly {s}\n{e}");
                    return new Type[0];
                }
            }).Where(p =>
            {
                try
                {
                    var allSrcs = p.GetInterfaces().Select(x => x.Name).AddItem(p.BaseType.Name);
                    if (BasicIMod.VerboseMode) LogUtils.DoVerboseLog($"srcs {p.Name} => {string.Join("; ", allSrcs)}");
                    var result = allSrcs.Any(x => x == t.Name) && p.IsClass && !p.IsAbstract;
                    return result;
                }
                catch { return false; }
            }).Select(x =>
            {
                try
                {
                    if (BasicIMod.TraceMode) LogUtils.DoTraceLog("Trying to instantiate '{0}'", x.AssemblyQualifiedName);
                    return x.GetConstructor(new Type[0]).Invoke(new object[0]);
                }
                catch (Exception e)
                {
                    LogUtils.DoInfoLog("Failed instantiate '{0}': {1}", x.AssemblyQualifiedName, e);
                    return null;
                }
            }).Where(x => x != null).ToArray();
        }
        public static T[] GetAllLoadableClassesInAppDomain<T>() where T : class
        {
            return GetAllLoadableClassesInAssemblyList(typeof(T)).Cast<T>().ToArray();
        }

        public static T[] GetAllLoadableClassesByTypeName<T, U>(Func<U> destinationGenerator, Assembly targetAssembly = null) where T : class where U : T
        {
            var classNameBase = typeof(T).FullName;
            var allTypes = GetAllInterfacesWithTypeName(classNameBase);
            if (BasicIMod.DebugMode) LogUtils.DoLog("Classes with same name of '{0}' found: {1}", classNameBase, allTypes.Length);
            return allTypes
                   .SelectMany(x =>
                   {
                       var res = GetAllLoadableClassesInAssemblyList(x, new[] { targetAssembly });
                       if (BasicIMod.TraceMode) LogUtils.DoTraceLog("Objects loaded: {0}", res.Length);
                       return res;
                   })
                   .Select(x => TryConvertClass<T, U>(x, destinationGenerator))
                   .Where(x => x != null).ToArray();
        }

        public static Type[] GetAllInterfacesWithTypeName(string typeName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            return assemblies.SelectMany(s =>
            {
                try
                {
                    return s.GetExportedTypes();
                }
                catch
                {
                    return new Type[0];
                }
            }).Where(p =>
            {
                try
                {
                    return p.FullName == typeName && p.IsInterface;
                }
                catch { return false; }
            }).ToArray();
        }

        public static T TryConvertClass<T, U>(object srcValue, Func<U> destinationGenerator) where U : T
        {
            if (BasicIMod.TraceMode) LogUtils.DoTraceLog("Trying to convert {0} to class {1}", srcValue.GetType().FullName, typeof(T).FullName);
            try
            {
                if (srcValue.GetType().IsAssignableFrom(typeof(T)))
                {
                    return (T)srcValue;
                }
                var newInstanceOfT = destinationGenerator();
                var srcType = srcValue.GetType();
                var dstType = newInstanceOfT.GetType();
                foreach (var fieldOnInterface in typeof(T).GetProperties(RedirectorUtils.allFlags))
                {
                    if (BasicIMod.VerboseMode) LogUtils.DoVerboseLog("fieldOnItf: {0} {1}=>{2}", fieldOnInterface.GetReturnType().FullName, typeof(T).FullName, fieldOnInterface.Name);
                    var fieldOnSrc = srcType.GetProperty(fieldOnInterface.Name, RedirectorUtils.allFlags);
                    if (BasicIMod.VerboseMode) LogUtils.DoVerboseLog("fieldOnSrc: {0} {1}=>{2}", fieldOnSrc.GetReturnType().FullName, srcType.FullName, fieldOnSrc.Name);
                    var fieldOnDst = dstType.GetProperty(fieldOnInterface.Name, RedirectorUtils.allFlags);
                    if (BasicIMod.VerboseMode) LogUtils.DoVerboseLog("fieldOnDst: {0} {1}=>{2}", fieldOnDst.GetReturnType().FullName, dstType.FullName, fieldOnDst.Name);
                    if (fieldOnSrc is not null && fieldOnSrc.PropertyType.IsAssignableFrom(fieldOnDst.PropertyType))
                    {
                        fieldOnDst.SetValue(newInstanceOfT, fieldOnSrc.GetValue(srcValue));
                    }
                }
                return newInstanceOfT;
            }
            catch (Exception e)
            {
                LogUtils.DoWarnLog("Error trying to convert class:\n{0}", e);
                return default;
            }
        }
    }
}
