#if BEPINEX_CS2
using System.IO;
using System.Linq;
#endif
using Belzont.Interfaces;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Unity.Entities;
using UnityEngine;

namespace Belzont.Utils
{
    public sealed class RedirectorUtils
    {
        public static readonly BindingFlags allFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.GetField | BindingFlags.GetProperty;
    }

    public interface IRedirectableWorldless
    {
    }
    public interface IRedirectable
    {
        void DoPatches(World world);
    }

    public class Redirector : MonoBehaviour
    {
        #region Class Base
        private static Harmony m_harmony;

        private static readonly List<MethodInfo> m_patches = new List<MethodInfo>();
        private static readonly List<Action> m_onUnpatchActions = new List<Action>();

        private readonly List<MethodInfo> m_detourList = new List<MethodInfo>();


        public static Harmony Harmony
        {
            get
            {
#if BEPINEX_CS2
                if (m_harmony == null)
                {
                    var modImplementations = ReflectionUtils.GetSubtypesRecursive(typeof(BasicIMod), null);
                    m_harmony = new Harmony($"com.klyte.redirectors.{modImplementations.First().Name}");
                }
                return m_harmony;
#else
                return m_harmony ??= new Harmony($"com.klyte.redirectors.{BasicIMod.Instance.Acronym}");
#endif
            }
        }
        #endregion

        public static readonly MethodInfo semiPreventDefaultMI = new Func<bool>(() =>
        {
            var stackTrace = new StackTrace();
            StackFrame[] stackFrames = stackTrace.GetFrames();
            if (BasicIMod.VerboseMode) LogUtils.DoVerboseLog($"SemiPreventDefault fullStackTrace: \r\n {Environment.StackTrace}");
            for (int i = 2; i < stackFrames.Length; i++)
            {
                if (stackFrames[i].GetMethod().DeclaringType.ToString().StartsWith("Klyte."))
                {
                    return false;
                }
            }
            return true;
        }).Method;

        public static bool PreventDefault() => false;

        public void AddRedirect(MethodInfo oldMethod, MethodInfo newMethodPre, MethodInfo newMethodPost = null, MethodInfo transpiler = null)
            => AddRedirect(oldMethod, newMethodPre, newMethodPost, transpiler, Priority.Normal, Priority.Normal);

        /// <summary>
        /// Same as <see cref="AddRedirect(MethodInfo, MethodInfo, MethodInfo, MethodInfo)"/> with explicit Harmony priorities.
        /// Comparison-based prefixes (snapshot before original) should use <see cref="Priority.First"/>.
        /// Update-group postfixes that observe final state (and use <c>bool __runOriginal</c>) should use
        /// <see cref="Priority.Last"/> so they run after other mods' After patches.
        /// </summary>
        public void AddRedirect(
            MethodInfo oldMethod,
            MethodInfo newMethodPre,
            MethodInfo newMethodPost,
            MethodInfo transpiler,
            int prefixPriority,
            int postfixPriority)
        {
            if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"Adding patch! {oldMethod.DeclaringType} {oldMethod}");
            var pre = newMethodPre != null
                ? new HarmonyMethod(newMethodPre) { priority = prefixPriority }
                : null;
            var post = newMethodPost != null
                ? new HarmonyMethod(newMethodPost) { priority = postfixPriority }
                : null;
            var transp = transpiler != null ? new HarmonyMethod(transpiler) : null;
            m_detourList.Add(Harmony.Patch(oldMethod, pre, post, transp));
            m_patches.Add(oldMethod);
        }

        public void AddReversePatch(MethodInfo targetMethod, MethodInfo ownMethod)
        {
            if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"Reverse patch! {targetMethod.DeclaringType} {targetMethod}");
            Harmony.ReversePatch(targetMethod, new HarmonyMethod(ownMethod));
        }
        public void AddUnpatchAction(Action unpatchAction) => m_onUnpatchActions.Add(unpatchAction);

        public static void UnpatchAll()
        {
            LogUtils.DoInfoLog($"Unpatching all: {Harmony.Id}");
            foreach (MethodInfo method in m_patches)
            {
                Harmony.Unpatch(method, HarmonyPatchType.All, Harmony.Id);
            }
            foreach (Action action in m_onUnpatchActions)
            {
                action?.Invoke();
            }
            m_onUnpatchActions.Clear();
            m_patches.Clear();
            var objName = $"k45_Redirectors_{Harmony.Id}";
            DestroyImmediate(GameObject.Find(objName));
        }
        public static void PatchAll()
        {
            LogUtils.DoInfoLog($"Patching all: {Harmony.Id}");
            var objName = $"k45_Redirectors_{Harmony.Id}";
            GameObject m_topObj = GameObject.Find(objName) ?? new GameObject(objName);
            DontDestroyOnLoad(m_topObj);
            Type typeTarg = typeof(IRedirectable);


#if BEPINEX_CS2
            string basePathLocation = Path.GetDirectoryName(typeof(Redirector).Assembly.Location);
            List<Assembly> refAssemblies = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies().Where(x =>
            {
                try
                {
                    return x.Location.StartsWith(basePathLocation);
                }
                catch { return false; }
            }));
#else
            List<Assembly> refAssemblies = new List<Assembly> { BasicIMod.Instance.GetType().Assembly };
#endif

            List<Type> instances = ReflectionUtils.GetInterfaceImplementations(typeTarg, refAssemblies);
            if (BasicIMod.DebugMode) LogUtils.DoDebugLog($"Found Redirectors: {instances.Count}");
            Type typeTargWorldless = typeof(IRedirectableWorldless);
            List<Type> instancesWorldless = ReflectionUtils.GetInterfaceImplementations(typeTargWorldless, refAssemblies);
            if (BasicIMod.DebugMode) LogUtils.DoDebugLog($"Found Worldless Redirectors: {instances.Count}");
            Application.logMessageReceived += ErrorPatchingHandler;
            try
            {
                foreach (Type t in instances)
                {
                    if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"Redirector: {t}");
                    worldDependantRedirectors.Add(m_topObj.AddComponent(t) as IRedirectable);
                }
                foreach (Type t in instancesWorldless)
                {
                    if (BasicIMod.TraceMode) LogUtils.DoTraceLog($"Redirector Worldless: {t}");
                    m_topObj.AddComponent(t);
                }
            }
            finally
            {
                Application.logMessageReceived -= ErrorPatchingHandler;
            }
        }

        private static readonly List<IRedirectable> worldDependantRedirectors = new();

        public static void OnWorldCreated(World world)
        {
            foreach (var redirector in worldDependantRedirectors)
            {
                redirector.DoPatches(world);
            }
        }

        private static void ErrorPatchingHandler(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Exception)
            {
                LogUtils.DoErrorLog($"{logString}\n{stackTrace}");
            }
        }

        public void EnableDebug() => HarmonyLib.Harmony.DEBUG = true;
        public void DisableDebug() => HarmonyLib.Harmony.DEBUG = false;
    }
}
