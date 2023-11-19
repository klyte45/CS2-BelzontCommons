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
        private static Harmony m_harmony = new Harmony($"com.klyte.redirectors.{IBasicIMod.Instance.Acronym}");
        private static readonly List<MethodInfo> m_patches = new List<MethodInfo>();
        private static readonly List<Action> m_onUnpatchActions = new List<Action>();

        private readonly List<MethodInfo> m_detourList = new List<MethodInfo>();


        public static Harmony Harmony
        {
            get
            {
                if (m_harmony is null)
                {
                    m_harmony = new Harmony($"com.klyte.redirectors.{IBasicIMod.Instance.Acronym}");
                }
                return m_harmony;
            }
        }
        #endregion

        public static readonly MethodInfo semiPreventDefaultMI = new Func<bool>(() =>
        {
            var stackTrace = new StackTrace();
            StackFrame[] stackFrames = stackTrace.GetFrames();
            LogUtils.DoLog($"SemiPreventDefault fullStackTrace: \r\n {Environment.StackTrace}");
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
        {

            LogUtils.DoLog($"Adding patch! {oldMethod.DeclaringType} {oldMethod}");
            m_detourList.Add(Harmony.Patch(oldMethod, newMethodPre != null ? new HarmonyMethod(newMethodPre) : null, newMethodPost != null ? new HarmonyMethod(newMethodPost) : null, transpiler != null ? new HarmonyMethod(transpiler) : null));
            m_patches.Add(oldMethod);
        }
        public void AddUnpatchAction(Action unpatchAction) => m_onUnpatchActions.Add(unpatchAction);

        public static void UnpatchAll()
        {
            LogUtils.DoWarnLog($"Unpatching all: {Harmony.Id}");
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
            LogUtils.DoWarnLog($"Patching all: {Harmony.Id}");
            var objName = $"k45_Redirectors_{Harmony.Id}";
            GameObject m_topObj = GameObject.Find(objName) ?? new GameObject(objName);
            DontDestroyOnLoad(m_topObj);
            Type typeTarg = typeof(IRedirectable);
            List<Type> instances = ReflectionUtils.GetInterfaceImplementations(typeTarg, new List<Assembly> { IBasicIMod.Instance.GetType().Assembly });
            LogUtils.DoLog($"Found Redirectors: {instances.Count}");
            Type typeTargWorldless = typeof(IRedirectableWorldless);
            List<Type> instancesWorldless = ReflectionUtils.GetInterfaceImplementations(typeTargWorldless, new List<Assembly> { IBasicIMod.Instance.GetType().Assembly });
            LogUtils.DoLog($"Found Worldless Redirectors: {instances.Count}");
            Application.logMessageReceived += ErrorPatchingHandler;
            try
            {
                foreach (Type t in instances)
                {
                    LogUtils.DoLog($"Redirector: {t}");
                    worldDependantRedirectors.Add(m_topObj.AddComponent(t) as IRedirectable);
                }
                foreach (Type t in instancesWorldless)
                {
                    LogUtils.DoLog($"Redirector Worldless: {t}");
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
