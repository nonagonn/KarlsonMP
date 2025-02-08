using MInject;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Launcher
{
    public partial class Launcher : Form
    {
        #region >>> PInvoke
        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);
        const uint WM_KEYDOWN = 0x0100;
        #endregion

        public Launcher()
        {
            InitializeComponent();
        }

        private void Launcher_Load(object sender, EventArgs e)
        {
            Properties.Settings.Default.Reload();

            textBox1.Text = Properties.Settings.Default.Username;
            textBox2.Text = Properties.Settings.Default.Address;
            checkBox1.Checked = Properties.Settings.Default.Offline;
            if (!checkBox1.Checked)
            {
                label1.Visible = false;
                textBox1.Visible = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Username = textBox1.Text;
            Properties.Settings.Default.Address = textBox2.Text;
            Properties.Settings.Default.Save();
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Karlson.exe")))
            {
                MessageBox.Show("Karlson was not detected in cwd.");
                return;
            }
            Process karlson;
            if (!checkBox1.Checked)
            {
                karlson = new Process
                {
                    StartInfo = new ProcessStartInfo(Path.Combine(Directory.GetCurrentDirectory(), "Karlson.exe"))
                    {
                        Arguments = '"' + textBox2.Text + '"'
                    }
                };
            }
            else
            {
                karlson = new Process
                {
                    StartInfo = new ProcessStartInfo(Path.Combine(Directory.GetCurrentDirectory(), "Karlson.exe"))
                    {
                        Arguments = $"linux \"{textBox2.Text}\" \"{textBox1.Text}\" 0"
                    }
                };
            }
            
            karlson.Start();
            while (karlson.MainWindowHandle == IntPtr.Zero) Thread.Sleep(0);
            PostMessage(karlson.MainWindowHandle, WM_KEYDOWN, 0xD, 0); // enter key
            Thread.Sleep(50);

            // run MInject
            // Method: Kernel.Kernel.Start()
            if (MonoProcess.Attach(karlson, out MonoProcess m_karlson))
            {
                byte[] assemblyBytes = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "KMP", "Kernel.dll"));
                IntPtr monoDomain = m_karlson.GetRootDomain();
                m_karlson.ThreadAttach(monoDomain);
                m_karlson.SecuritySetMode(0);
                m_karlson.DisableAssemblyLoadCallback();

                IntPtr rawAssemblyImage = m_karlson.ImageOpenFromDataFull(assemblyBytes);
                IntPtr assemblyPointer = m_karlson.AssemblyLoadFromFull(rawAssemblyImage);
                IntPtr assemblyImage = m_karlson.AssemblyGetImage(assemblyPointer);
                IntPtr classPointer = m_karlson.ClassFromName(assemblyImage, "Kernel", "Kernel");
                IntPtr methodPointer = m_karlson.ClassGetMethodFromName(classPointer, "Inject");

                m_karlson.RuntimeInvoke(methodPointer);
                m_karlson.EnableAssemblyLoadCallback();
                m_karlson.Dispose();
            }
            else
            {
                karlson.Kill();
                MessageBox.Show("Couldn't execute MInject.\nPlease retry");
                return;
            }
            Process.GetCurrentProcess().Kill();
            return;
        }

        private void Launcher_FormClosing(object sender, FormClosingEventArgs e)
        {
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            label1.Visible = checkBox1.Checked;
            textBox1.Visible = checkBox1.Checked;
            Properties.Settings.Default.Offline = checkBox1.Checked;
            Properties.Settings.Default.Save();
        }
    }
}
