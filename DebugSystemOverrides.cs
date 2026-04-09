using Belzont.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Belzont.Overrides
{
    /// <summary>
    /// Patches Game.Debug.DebugSystem.BuildSimulationDebugUI to skip abstract ToolBaseSystem
    /// subtypes when building the tool-selection array, preventing a crash from
    /// World.GetOrCreateSystemManaged being called with an abstract type.
    ///
    /// Strategy: inject a FilterAbstractTypes call immediately after GetAllTypesDerivedFromAsArray
    /// stores its result in V_1 (local index 1). This pre-filters the array before the loop,
    /// so ToolBaseSystem[] (V_2) and GUIContent[] (V_3) are both sized correctly from the start.
    ///
    /// Safety: the whole for-loop body is matched against the expected unpatched opcodes first.
    /// If the sequence differs (another mod already patched the loop), we skip our injection
    /// to avoid double-patching conflicts.
    /// </summary>
    public class DebugSystemOverrides : Redirector, IRedirectableWorldless
    {
        public void Awake()
        {
            var target = AccessTools.Method("Game.Debug.DebugSystem:BuildSimulationDebugUI");
            if (target != null)
                AddRedirect(target, null, null,
                    GetType().GetMethod(nameof(Transpiler_BuildSimulationDebugUI), RedirectorUtils.allFlags));
        }

        // Expected opcode sequence for the unpatched for-loop body (IL instructions 40–63).
        // If another mod has already modified this loop the sequence will differ and we skip.
        private static readonly OpCode[] s_expectedLoopOpcodes = new OpCode[]
        {
            // load allTypesDerivedFromAsArray[i] → stloc V_6 (type)
            OpCodes.Ldloc_1, OpCodes.Ldloc_S, OpCodes.Ldelem_Ref, OpCodes.Stloc_S,
            // array[i] = World.GetOrCreateSystemManaged(type) cast to ToolBaseSystem
            OpCodes.Ldloc_2, OpCodes.Ldloc_S, OpCodes.Ldarg_0,
            OpCodes.Call /* get_World */, OpCodes.Ldloc_S, OpCodes.Callvirt /* GetOrCreate */,
            OpCodes.Castclass, OpCodes.Stelem_Ref,
            // array2[i] = new GUIContent(array[i].toolID)
            OpCodes.Ldloc_3, OpCodes.Ldloc_S, OpCodes.Ldloc_2, OpCodes.Ldloc_S,
            OpCodes.Ldelem_Ref, OpCodes.Callvirt /* get_toolID */, OpCodes.Newobj, OpCodes.Stelem_Ref,
            // i++
            OpCodes.Ldloc_S, OpCodes.Ldc_I4_1, OpCodes.Add, OpCodes.Stloc_S,
        };

        private static IEnumerable<CodeInstruction> Transpiler_BuildSimulationDebugUI(
            IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            // Step 1: Verify the for-loop body matches the unpatched expected sequence.
            // If it doesn't (another mod already patched), skip our injection.
            bool loopBodyFound = false;
            for (int i = 0; i <= codes.Count - s_expectedLoopOpcodes.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < s_expectedLoopOpcodes.Length; j++)
                {
                    if (codes[i + j].opcode != s_expectedLoopOpcodes[j]) { match = false; break; }
                }
                if (match) { loopBodyFound = true; break; }
            }

            if (!loopBodyFound)
            {
                LogUtils.DoWarnLog(
                    "[DebugSystemOverrides] For-loop body does not match expected unpatched opcodes " +
                    "— another mod may have already patched it. Injection skipped.");
                return codes;
            }

            // Step 2: Find the injection point — the stloc.1 immediately following the
            // call to GetAllTypesDerivedFromAsArray.  We inject right after it so that V_1
            // already holds the raw (unfiltered) array when our helper replaces it.
            int stloc1Idx = -1;
            for (int i = 0; i < codes.Count - 1; i++)
            {
                if (codes[i].opcode == OpCodes.Call
                    && codes[i].operand is MethodInfo mi
                    && mi.Name.Contains("GetAllTypesDerivedFromAsArray")
                    && codes[i + 1].opcode == OpCodes.Stloc_1)
                {
                    stloc1Idx = i + 1; // index of stloc.1
                    break;
                }
            }

            if (stloc1Idx < 0)
            {
                LogUtils.DoWarnLog(
                    "[DebugSystemOverrides] Could not locate GetAllTypesDerivedFromAsArray + stloc.1. " +
                    "Injection skipped.");
                return codes;
            }

            var filterMethod = typeof(DebugSystemOverrides)
                .GetMethod(nameof(FilterAbstractTypes), RedirectorUtils.allFlags);

            // Inject after stloc.1:
            //   ldloc.1                 — reload V_1 (allTypesDerivedFromAsArray)
            //   call FilterAbstractTypes — returns filtered Type[] (no abstract, no null)
            //   stloc.1                 — overwrite V_1 with filtered array
            codes.InsertRange(stloc1Idx + 1, new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Call, filterMethod),
                new CodeInstruction(OpCodes.Stloc_1),
            });

            return codes;
        }

        /// <summary>
        /// Removes abstract and null entries from the type array returned by
        /// GetAllTypesDerivedFromAsArray before the loop allocates ToolBaseSystem[]
        /// and GUIContent[] — so both derived arrays are sized correctly from the start.
        /// </summary>
        private static Type[] FilterAbstractTypes(Type[] types)
            => Array.FindAll(types, t => t != null && !t.IsAbstract);
    }
}
