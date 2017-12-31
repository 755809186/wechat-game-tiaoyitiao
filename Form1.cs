using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TiaoYiTiao
{
    public partial class Form1 : Form
    {
        #region fields
        public Form1()
        {
            InitializeComponent();
        }
        enum MouseState
        {
            None = 0,
            MouseLeftDown = 1,
            MouseRightDown = 2,
        }

        /// <summary>
        /// 是否停止刷新界面
        /// </summary>
        private bool isStop = false;
        /// <summary>
        /// 是否存在安卓
        /// </summary>
        private bool HasAndroid = false;

        /// <summary>
        /// 截屏后的文件
        /// </summary>
        string scPic = "1.png";
        #endregion

        #region form load
        /// <summary>
        /// 设备后插入延时执行
        /// </summary>
        private System.Timers.Timer _timer = new System.Timers.Timer(1200);
        private void Form1_Load(object sender, EventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                _timer.AutoReset = false;//只需要执行一次
                _timer.Elapsed += (o, e1) => { CheckHasAndroidModel(); };
                _timer.Start();
            });
            //CheckHasAndroidModel();

            var tmp = Path.Combine(Environment.CurrentDirectory, "temp");
            if (!Directory.Exists(tmp))
                Directory.CreateDirectory(tmp);

            Environment.CurrentDirectory = tmp;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (File.Exists(scPic)) File.Delete(scPic);

            string path = Path.GetDirectoryName(Program.AdbPath);
            var fs = Directory.GetFiles(path);
            foreach (var f in fs)
            {
                try
                {
                    File.Delete(f);
                }
                catch { }
            }
        }
        #endregion

        #region 鼠标单击
        /// <summary>
        /// 黑人底部位置
        /// </summary>
        Point _start;
        /// <summary>
        /// 图案中心或者白点位置
        /// </summary>
        Point _end;
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            var me = ((MouseEventArgs)(e));
            if (me.Button==MouseButtons.Left)//按下左键是黑人底部的坐标
            {
                _start = ((MouseEventArgs)(e)).Location;
                //toolStripStatusLabel1.Text = "左健点击起始位置，右键点击结束位置";
            }
            else if (me.Button == MouseButtons.Right)//按下右键键是黑人底部的坐标
            {
                _end = ((MouseEventArgs)(e)).Location;
                //计算两点直接的距离
                double value = Math.Sqrt(Math.Abs(_start.X - _end.X) * Math.Abs(_start.X - _end.X) + Math.Abs(_start.Y - _end.Y) * Math.Abs(_start.Y - _end.Y));

                //this.Text = string.Format("两点之间的距离：{0}，需要按下时间：{1}", value, (3.999022243950134 * value).ToString("0")); 
                //3.999022243950134  这个是我通过多次模拟后得到 我这个分辨率的最佳时间
                cmdAdb(string.Format("shell input swipe 100 100 200 200 {0}", (3.999022243950134 * value).ToString("0")));
            }
        }
        #endregion

        #region 设备插入或拔出
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x219)
            {
                Debug.WriteLine("WParam：{0} ,LParam:{1},Msg：{2}，Result：{3}", m.WParam, m.LParam, m.Msg, m.Result);
                if (m.WParam.ToInt32() == 7)//设备插入或拔出
                {
                    CheckHasAndroidModel();
                    _timer.Start();
                }
            }
            try
            {
                base.WndProc(ref m);
            }
            catch { }
        }
        #endregion

        #region 检测是否存在手机
        private void CheckHasAndroidModel()
        {
            var text = cmdAdb("shell getprop ro.product.model", false);//获取手机型号
            Debug.WriteLine("检查设备：" + text + "  T=" + DateTime.Now);
            if (text.Contains("no devices") || string.IsNullOrWhiteSpace(text))
            {
                HasAndroid = false;
                isStop = true;
                toolStripStatusLabel1.Text = "未检测到设备";
                
                using (Graphics g = pictureBox1.CreateGraphics())
                {
                    g.DrawString("未检测到设备。\r\n此功能需要用 USB 数据线连接安卓手机，\r\n并打开安卓手机的 USB 调试模式。", 
                        new Font("微软雅黑", 12), 
                        Brushes.Red, 
                        new PointF(10, 10));

                    g.Dispose();
                }
            }
            else
            {
                HasAndroid = true;
                isStop = false;
                toolStripStatusLabel1.Text = string.Format("【{0}】左健点击起始位置，右键点击结束位置", text.Trim());

                bg_worker();
            }
        }
        #endregion

        #region 后台执行截屏
        void bg_worker()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (isStop) break;

                    cmdAdb("shell screencap -p /sdcard/" + scPic);
                    cmdAdb("pull /sdcard/" + scPic);

                    if (!File.Exists(scPic)) continue;

                    using (var img = Image.FromFile(scPic))
                    {
                        pictureBox1.Invoke(new Action(() =>
                        {
                            pictureBox1.Image = new Bitmap(img);
                        }));
                        img.Dispose();
                    }
                    File.Delete(scPic);

                    GC.Collect();
                    Thread.Sleep(1000);
                }
            });
        }
        #endregion

        #region 设置窗口大小
        void setSize()
        {
            //var w = Screen.PrimaryScreen.WorkingArea.Width;
            var h = Screen.PrimaryScreen.WorkingArea.Height - 5;

            float rate = ((float)h / 1920f);
            this.Size = new Size((int)(1080 * rate), (int)(1920 * rate));
            this.StartPosition = FormStartPosition.CenterScreen;
        }
        #endregion

        #region 执行adb命令
        /// <summary>
        /// 执行adb命令
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="ischeck"></param>
        /// <returns></returns>
        private string cmdAdb(string arguments, bool ischeck = true)
        {
            if (ischeck && !HasAndroid)
            {
                return string.Empty;
            }
            string ret = string.Empty;
            using (Process p = new Process())
            {
                p.StartInfo.FileName = Program.AdbPath;
                p.StartInfo.Arguments = arguments;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;   //重定向标准输入   
                p.StartInfo.RedirectStandardOutput = true;  //重定向标准输出   
                p.StartInfo.RedirectStandardError = true;   //重定向错误输出   
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                ret = p.StandardOutput.ReadToEnd();
                p.Close();
            }
            return ret;
        }
        #endregion

        #region button
        // 菜单
        //private void button1_Click(object sender, EventArgs e)
        //{
        //    cmdAdb("shell input keyevent  82 ");
        //}

        //// Home
        //private void button2_Click(object sender, EventArgs e)
        //{
        //    cmdAdb("shell input keyevent  3 ");
        //}

        //// 返回
        //private void button3_Click(object sender, EventArgs e)
        //{
        //    cmdAdb("shell input keyevent 4 ");
        //}

        // 状态栏
        //private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        //{
        //    // 电源键
        //    cmdAdb("shell input keyevent 26 ");
        //}
        #endregion
    }
}
