using RTCV.CorruptCore;
using RTCV.NetCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vanguard;

namespace UnityStub
{
    public partial class StubForm : Form
    {

        public StubForm()
        {
            InitializeComponent();

            SyncObjectSingleton.SyncObject = this;


        Text += UnityWatch.UnityStubVersion;

            this.cbTargetType.Items.AddRange(new object[] {
                TargetType.UNITYEXE_UNITYDLL,
                TargetType.UNITYEXE_ALLDLL,
                TargetType.UNITYEXE,
                TargetType.UNITYEXE_KNOWNDLL,
                TargetType.UNITYENGINE,
                TargetType.ALLTHEGAME,
            });

        }

        private void BtnRestartStub_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }


        private void StubForm_Load(object sender, EventArgs e)
        {
            cbTargetType.SelectedIndex = 0;

            UnityWatch.Start();
        }

        public void RunProgressBar(string progressLabel, int maxProgress, Action<object, EventArgs> action, Action<object, EventArgs> postAction = null)
        {

            if (UnityWatch.progressForm != null)
            {
                UnityWatch.progressForm.Close();
                this.Controls.Remove(UnityWatch.progressForm);
                UnityWatch.progressForm = null;
            }

            UnityWatch.progressForm = new ProgressForm(progressLabel, maxProgress, action, postAction);
            UnityWatch.progressForm.Run();

        }




        Size originalLbTargetSize;
        Point originalLbTargetLocation;
        public void EnableInterface()
        {
            var diff = lbTarget.Location.X - btnBrowseTarget.Location.X;
            originalLbTargetLocation = lbTarget.Location;
            lbTarget.Location = btnBrowseTarget.Location;
            lbTarget.Visible = true;

            btnTargetSettings.Visible = false;

            btnBrowseTarget.Visible = false;
            originalLbTargetSize = lbTarget.Size;
            lbTarget.Size = new Size(lbTarget.Size.Width + diff, lbTarget.Size.Height);
            btnUnloadTarget.Visible = true;
            cbTargetType.Enabled = false;

            //lbTargetExecution.Enabled = true;
            //pnTargetExecution.Enabled = true;

            btnRestoreBackup.Enabled = true;
            btnResetBackup.Enabled = true;
            btnClearAllBackups.Enabled = true;

            lbTargetStatus.Text = UnityWatch.currentFileInfo.selectedTargetType.ToString() + " target loaded";
        }

        public void DisableInterface()
        {
            btnUnloadTarget.Visible = false;
            btnBrowseTarget.Visible = true;
            lbTarget.Size = originalLbTargetSize;
            lbTarget.Location = originalLbTargetLocation;
            lbTarget.Visible = false;
            cbTargetType.Enabled = true;

            btnTargetSettings.Visible = true;

            btnRestoreBackup.Enabled = false;
            btnResetBackup.Enabled = false;
            lbTarget.Text = "No target selected";
            lbTargetStatus.Text = "No target selected";
        }

        private void BtnBrowseTarget_Click(object sender, EventArgs e)
        {

            if (!UnityWatch.LoadTarget())
                return;

            if (!VanguardCore.vanguardConnected)
                VanguardCore.Start();

            EnableInterface();

        }

        private void BtnReleaseTarget_Click(object sender, EventArgs e)
        {
            UnityWatch.CloseTarget();
            DisableInterface();
        }

        private void CbTargetType_SelectedIndexChanged(object sender, EventArgs e)
        {
            //if(cbSelectedExecution.SelectedItem.ToString())
            UnityWatch.currentFileInfo.selectedTargetType = cbTargetType.SelectedItem.ToString();

        }

        private void BtnKillProcess_Click(object sender, EventArgs e)
        {
            UnityWatch.KillProcess();
        }

        private void BtnRestoreBackup_Click(object sender, EventArgs e)
        {
            UnityWatch.currentFileInfo.targetInterface?.CloseStream();
            UnityWatch.currentFileInfo.targetInterface?.RestoreBackup();
        }

        private void BtnResetBackup_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
@"This resets the backup of the current target by using the current data from it.
If you override a clean backup using a corrupted file,
you won't be able to restore the original file using it.

Are you sure you want to reset the current target's backup?", "WARNING", MessageBoxButtons.YesNo) == DialogResult.No)
                return;

            UnityWatch.currentFileInfo.targetInterface?.ResetBackup(true);

        }

        private void BtnClearAllBackups_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to clear ALL THE BACKUPS\n from UnityStub's cache?", "WARNING", MessageBoxButtons.YesNo) == DialogResult.No)
                return;


            UnityWatch.currentFileInfo.targetInterface?.RestoreBackup();

            foreach (string file in Directory.GetFiles(UnityWatch.currentDir + "\\TEMP"))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    MessageBox.Show($"Could not delete file {file}");
                }
            }

            FileInterface.CompositeFilenameDico = new Dictionary<string, string>();
            UnityWatch.currentFileInfo.targetInterface?.ResetBackup(false);
            FileInterface.SaveCompositeFilenameDico();
            MessageBox.Show("All the backups were cleared.");
        }

        private void BtnTargetSettings_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Control c = (Control)sender;
                Point locate = new Point(c.Location.X + e.Location.X, ((Control)sender).Location.Y + e.Location.Y);

                ContextMenuStrip columnsMenu = new ContextMenuStrip();


                ((ToolStripMenuItem)columnsMenu.Items.Add("Big endian", null, new EventHandler((ob, ev) => {

                    UnityWatch.currentFileInfo.bigEndian = !UnityStub.UnityWatch.currentFileInfo.bigEndian;

                    if (VanguardCore.vanguardConnected)
                        UnityWatch.UpdateDomains();

                }))).Checked = UnityWatch.currentFileInfo.bigEndian;

                ((ToolStripMenuItem)columnsMenu.Items.Add("Auto-Uncorrupt", null, new EventHandler((ob, ev) => {

                    UnityWatch.currentFileInfo.autoUncorrupt = !UnityWatch.currentFileInfo.autoUncorrupt;

                }))).Checked = UnityWatch.currentFileInfo.autoUncorrupt;

                ((ToolStripMenuItem)columnsMenu.Items.Add("Use Caching + Multithreading", null, new EventHandler((ob, ev) => {

                    UnityWatch.currentFileInfo.useCacheAndMultithread = !UnityWatch.currentFileInfo.useCacheAndMultithread;

                }))).Checked = UnityWatch.currentFileInfo.useCacheAndMultithread;

                columnsMenu.Show(this, locate);
            }
        }

        private void BtnExecutionSettings_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private void StubForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnityWatch.CloseTarget(false);
        }
    }
}
