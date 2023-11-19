using Belzont.Interfaces;
using Colossal.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace Belzont.Utils
{
    public static class LogUtils
    {
        #region Log Utils

        private static ILog logOutput;

        private static ILog LogOutput
        {
            get
            {
                if (logOutput is null)
                {
                    logOutput = LogManager.GetLogger("Mods_K45");
                    logOutput.effectivenessLevel = Level.Info;
                }
                return logOutput;
            }
        }

        private static string LogLineStart(string level) => $"[{IBasicIMod.Instance.Acronym,-4}] [v{IBasicIMod.FullVersion,-16}] [{level,-8}] ";

        public static void DoLog(string format, params object[] args)
        {
            try
            {
                if (IBasicIMod.DebugMode)
                {
                    var oldEffectivenessLevel = LogOutput.effectivenessLevel;
                    LogOutput.effectivenessLevel = Level.Debug;
                    LogOutput.Log(Level.Debug, string.Format(LogLineStart("DEBUG") + format, args), null);
                    LogOutput.effectivenessLevel = oldEffectivenessLevel;
                }

            }
            catch (Exception e)
            {
                LogCaughtLogException(format, args, e);
            }
        }
        public static void DoWarnLog(string format, params object[] args)
        {
            try
            {
                LogOutput.Log(Level.Warn, string.Format(LogLineStart("WARNING") + format, args), null);
            }
            catch (Exception e)
            {
                LogCaughtLogException(format, args, e);
            }
        }

        private static void LogCaughtLogException(string format, object[] args, Exception e)
        {
            LogOutput.Log(Level.Warn, string.Format($"{LogLineStart("SEVERE")} Erro ao fazer log: {{0}} (args = {{1}})", format, args == null ? "[]" : string.Join(",", args.Select(x => x != null ? x.ToString() : "--NULL--").ToArray())), e);
        }

        public static void DoInfoLog(string format, params object[] args)
        {
            try
            {
                LogOutput.Log(Level.Info, string.Format(LogLineStart("INFO") + format, args), null);
            }
            catch (Exception e)
            {
                LogCaughtLogException(format, args, e);
            }
        }
        public static void DoErrorLog(string format, Exception e = null, params object[] args)
        {
            try
            {
                LogOutput.Log(Level.Error, string.Format(LogLineStart("ERROR") + format, args), e);
            }
            catch (Exception e2)
            {
                if (e != null)
                {
                    LogOutput.Log(Level.Error, LogLineStart("ERROR") + "An exception has occurred.", e);
                }
                LogCaughtLogException(format, args, e2);
            }
        }

        public static void PrintMethodIL(IEnumerable<CodeInstruction> inst, bool force = false)
        {
            if (force || IBasicIMod.DebugMode)
            {
                int j = 0;
                LogOutput.Log(Level.Info, $"{LogLineStart("TRANSPILLED")}\n\t{string.Join("\n\t", inst.Select(x => $"{j++:D8} {x.opcode,-10} {ParseOperand(inst, x.operand)}").ToArray())}", null);
            }
        }

        public static string GetLinesPointingToLabel(IEnumerable<CodeInstruction> inst, Label lbl)
        {
            int j = 0;
            return "\t" + string.Join("\n\t", inst.Select(x => Tuple.New(x, $"{(j++).ToString("D8")} {x.opcode.ToString().PadRight(10)} {ParseOperand(inst, x.operand)}")).Where(x => x.First.operand is Label label && label == lbl).Select(x => x.Second).ToArray());
        }


        public static string ParseOperand(IEnumerable<CodeInstruction> instr, object operand)
        {
            if (operand is null)
            {
                return null;
            }

            if (operand is Label lbl)
            {
                return "LBL: " + instr.Select((x, y) => Tuple.New(x, y)).Where(x => x.First.labels.Contains(lbl)).Select(x => $"{x.Second:D8} {x.First.opcode,-10} {ParseOperand(instr, x.First.operand)}").FirstOrDefault();
            }
            else
            {
                return operand.ToString() + $" (Type={operand.GetType()})";
            }
        }
        #endregion
    }
}
