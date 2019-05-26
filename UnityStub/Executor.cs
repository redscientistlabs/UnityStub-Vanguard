using RTCV.CorruptCore;
using RTCV.NetCore.StaticTools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UnityStub
{
    static class Executor
    {
        public static string unityExeFile = null;
        public static string script = null;

        public static void Execute()
        {

                if (unityExeFile != null)
                {

                    string fullPath = unityExeFile;
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = Path.GetFileName(fullPath);
                    psi.WorkingDirectory = Path.GetDirectoryName(fullPath);

                    Process.Start(psi);

                }
                else
                    MessageBox.Show("You need to specify a file to execute with the Edit Exec button.");
                return;

        }

    }


}
