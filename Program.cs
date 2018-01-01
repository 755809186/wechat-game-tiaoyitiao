using System;
using System.IO;
using System.Windows.Forms;

namespace TiaoYiTiao
{
    /// <summary>
    /// 微信 跳一跳 游戏辅助程序。
    /// </summary>
    static class Program
    {
        internal static string AdbPath = "";

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                loadDll();
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                MessageBox.Show("系统异常\r\n" + ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region loadDll
        static void loadDll()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //string path = Application.StartupPath;
            //string path = Application.CommonAppDataPath; // C:\ProgramData\TiaoYiTiao\ShowAndroidModel\1.0.0.0
            string path = Path.Combine(Path.GetTempPath(), "bao_tiaoyitiao"); // C:\Users\Bao\AppData\Local\Temp\bao_tiaoyitiao
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            string[] resources = new string[3] { "adb.exe", "AdbWinApi.dll", "AdbWinUsbApi.dll" };

            FileStream fs = null;
            string file;
            foreach (var r in resources)
            {
                file = Path.Combine(path, r);
                if (File.Exists(file)) continue;

                fs = new FileStream(file, FileMode.CreateNew, FileAccess.Write);
                byte[] buffer = Properties.Resources.ResourceManager.GetObject(Path.GetFileNameWithoutExtension(r)) as byte[];
                fs.Write(buffer, 0, buffer.Length);
                fs.Close();
            }

            if (fs != null)
            {
                fs.Close();
                fs.Dispose();
            }

            AdbPath = Path.Combine(path, "adb.exe");
        }
        #endregion
    }
}
