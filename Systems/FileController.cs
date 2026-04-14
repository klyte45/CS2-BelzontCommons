using System;
using System.IO;
using System.Linq;

namespace Belzont.Systems
{
    public abstract partial class BaseFileController : BindableSystemBase
    {
        private const string PREFIX = "file.";

        public override sealed void SetupCallBinder(Action<string, Delegate> callBinder)
        {
            callBinder($"{PREFIX}listFiles", ListFiles);
            SetupOtherCallBinder(callBinder);
        }

        public virtual void SetupOtherCallBinder(Action<string, Delegate> callBinder) { }

        protected class ListFileResult
        {
            public string displayName;
            public bool directory;
            public string fullPath;
        }

        protected virtual ListFileResult[] ListFiles(string folder, string allowedExtensions)
        {
            return Directory.Exists(folder)
                ? [
                    .. Directory.GetDirectories(folder)
                                        .Select(x => new ListFileResult
                                        {
                                            displayName = Path.GetFileName(x),
                                            directory = true,
                                            fullPath = x
                                        }),
                    .. allowedExtensions.Split("|").SelectMany(ext =>
                        Directory.GetFiles(folder, ext)
                        .Select(x => new ListFileResult
                        {
                            displayName = Path.GetFileName(x),
                            directory = false,
                            fullPath = x
                        })).OrderBy(x => x.displayName)
,
                ] : null;
        }

    }
}