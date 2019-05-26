using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using System.Drawing;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.IO;
using Ceras;
using RTCV.CorruptCore;

namespace UnityStub
{

    public class UnityStubFileInfo
    {
        internal string targetShortName = "No target";
        internal bool writeCopyMode = false;
        internal string targetFullName = "No target";
        internal FileMemoryInterface targetInterface;
        internal string selectedTargetType = TargetType.UNITYEXE_UNITYDLL;
        internal bool autoUncorrupt = true;
        internal bool TerminateBeforeExecution = true;
        internal bool useAutomaticBackups = true;
        internal bool bigEndian = false;
        internal bool useCacheAndMultithread = true;

        public override string ToString()
        {
            return targetShortName;
        }
    }

    public static class TargetType
    {
        public const string UNITYEXE_ALLDLL = "Unity EXE and all DLLs";
        public const string UNITYEXE_UNITYDLL = "Unity EXE and unity DLLs";
        public const string UNITYEXE_KNOWNDLL = "Unity EXE and gameplay DLLs";
        public const string UNITYEXE = "Unity EXE";
        public const string UNITYENGINE = "UnityEngine.dll";
        public const string ALLTHEGAME = "The entire game folder";
    }

}
