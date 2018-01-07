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
        /// 是否自动识别图像
        /// </summary>
        bool isAutoRecognize = false;

        /// <summary>
        /// 是否自动跳
        /// </summary>
        bool isAutoJump = false;

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
            this.toolStripStatusLabel2.Text = "";

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
            var ps = Process.GetProcessesByName("adb");
            foreach (var p in ps)
                p.Kill();

            if (File.Exists(scPic)) File.Delete(scPic);

            //string path = Path.GetDirectoryName(Program.AdbPath);
            //Directory.Delete(path, true);

            //var fs = Directory.GetFiles(path);
            //foreach (var f in fs)
            //{
            //    try
            //    {
            //        File.Delete(f);
            //    }
            //    catch { }
            //}
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
            this.chkRecognize.Checked = false;
            if (me.Button==MouseButtons.Left)//按下左键是黑人底部的坐标
            {
                if (_start.IsEmpty)
                {
                    MessageBox.Show("右健点击起始位置，左键点击结束位置");
                    return;
                }
                _end = ((MouseEventArgs)(e)).Location;
                toolStripStatusLabel2.Text = string.Format("起跳：{0}，目标：{1}", _start, _end);

                this.jump();
            }
            else if (me.Button == MouseButtons.Right)//按下右键键是黑人底部的坐标
            {
                _start = ((MouseEventArgs)(e)).Location;
                toolStripStatusLabel2.Text = string.Format("起跳：{0}，目标：{1}", _start, _end);
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
            if (text.Contains("* daemon not running. starting it now on port 5037 *\r\n* daemon started successfully *"))
                text = text.Replace("* daemon not running. starting it now on port 5037 *\r\n* daemon started successfully *", "");
            
            if (text.Contains("no devices") || string.IsNullOrWhiteSpace(text))
            {
                HasAndroid = false;
                isStop = true;
                Invoke(new MethodInvoker(() =>
                {
                    toolStripStatusLabel1.Text = "未检测到设备";
                }));
                
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
                Invoke(new MethodInvoker(() =>
                {
                    toolStripStatusLabel1.Text = string.Format("【{0}】", text.Trim());
                }));

                bg_worker();
            }
        }
        #endregion

        #region 后台循环执行
        // 每一次跳跃比上一次多 n 次，然后自杀一次
        int max_jump_count = 0; // 最多跳跃次数
        int jump_count = 0; // 本次跳跃次数
        int last_jump_count = 0; // 上次跳跃次数
        int per_jump_count= 0; // 每局跳跃的次数
        int[] per_jump_range = new int[] { 15, 30 };

        void bg_worker()
        {
            ImageHelper imageHelper = new ImageHelper(this.pictureBox1);
            
            Task.Factory.StartNew(() =>
            {
                Image img = null;
                while (true)
                {
                    if (isStop) break;

                    cmdAdb("shell screencap -p /sdcard/" + scPic);
                    cmdAdb("pull /sdcard/" + scPic);

                    if (!File.Exists(scPic)) continue;

                    using (img = Image.FromFile(scPic))
                    {
                        if (img == null)
                        {
                            continue;
                        }

                        #region 尝试自动识别
                        if (isAutoRecognize)
                        {
                            //autoRecognize(img);
                            var start_end_location = imageHelper.autoRecognize(img);
                            //if (start_end_location == null) continue;
                            _start = start_end_location[0];
                            _end = start_end_location[1];

                            //toolStripStatusLabel2.Text = string.Format("起跳：{0}，目标：{1}", _start, _end);
                        }
                        #endregion

                        #region 自动跳
                        if (isAutoJump)
                        {
                            // 随机生成每局跳跃的次数
                            if (max_jump_count > 0 && per_jump_count == 0)
                            {
                                if (last_jump_count + per_jump_range[1] > max_jump_count)
                                    per_jump_count = max_jump_count - last_jump_count;
                                else
                                    per_jump_count = new Random().Next(per_jump_range[0], per_jump_range[1]);
                            }

                            // 开始界面
                            if (_start.IsEmpty && _end.IsEmpty)
                            {
                                jump_count = 0;
                                tap_play_again();
                            }
                        }
                        #endregion

                        pictureBox1.Invoke(new Action(() =>
                        {
                            pictureBox1.Image = new Bitmap(img);
                            this.chkRecognize.Enabled = true;
                        }));

                        //img.Dispose();
                        img = null;
                    }
                    try
                    {
                        File.Delete(scPic);
                    }
                    catch { }

                    GC.Collect();

                    #region 自动跳 判断是继续跳，还是结束/自杀
                    if (isAutoJump && !_start.IsEmpty && !_end.IsEmpty)
                    {
                        Thread.Sleep(new Random().Next(800, 2000));
                        // 再次判断，防止在此时间取消自动跳
                        if (isAutoJump)
                        {
                            // 计算是否需要自杀一次
                            if (max_jump_count > 0 && (jump_count > last_jump_count + per_jump_count - 1))
                            {
                                last_jump_count = jump_count;
                                per_jump_count = 0;

                                if (jump_count > max_jump_count - 1)
                                {
                                    // 结束
                                    this.isAutoJump = false;
                                    btnJump_Click(this.btnJump, null);
                                }

                                kill_self();
                            }
                            else
                            {
                                jump();
                            }
                        }
                    }
                    #endregion
                }
                if (img != null) img.Dispose();
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
                p.Dispose();
            }
            return ret;
        }
        #endregion

        #region 跳
        private void jump()
        {
            jump_count++;
            if (isAutoJump && max_jump_count > 0)
                toolStripStatusLabel2.Text = string.Format("最多次数：{0}，本局次数：{1}/{2}", max_jump_count == 0 ? "不限" : max_jump_count.ToString(), jump_count, per_jump_count + last_jump_count);
            else
                toolStripStatusLabel2.Text = string.Format("本局次数：{0}", jump_count);

            //计算两点直接的距离
            double value = Math.Sqrt(Math.Abs(_start.X - _end.X) * Math.Abs(_start.X - _end.X) + Math.Abs(_start.Y - _end.Y) * Math.Abs(_start.Y - _end.Y));

            // 蓄力时间 = 距离 * 力度系数
            // “距离”就是棋子位置与跳跃落脚点位置的距离，根据上面的方法得出这两个位置的坐标点后，根据直角三角形的勾股定理即可求出，
            // 力度系数：
            // 计算公式：1495 / 手机分辨率宽度
            // 计算公式：2560 / 手机分辨率高度
            float coefficient;
            //coefficient = 3.999022243950134f;//  这个是我通过多次模拟后得到 我这个分辨率的最佳时间
            coefficient = (float)(1495f / (float)this.pictureBox1.Width);
            //coefficient = (float)(2560f / (float)this.pictureBox1.Height);

            double duration = coefficient * value;

            //toolStripStatusLabel2.Text = string.Format("两点之间的距离：{0}，需要按下时间：{1}", value, duration.ToString("0")); 
            cmdAdb(string.Format("shell input swipe 100 100 200 200 {0}", duration.ToString("0")));

            _start = Point.Empty;
            _end = Point.Empty;
            Thread.Sleep(2500);
        }

        private void kill_self()
        {
            cmdAdb(string.Format("shell input swipe 100 100 200 200 {0}", 1200));

            //_start = Point.Empty;
            //_end = Point.Empty;
            Thread.Sleep(3000);
        }

        // 
        private void tap_play_again()
        {
            cmdAdb("shell input tap 384 1564");
            Thread.Sleep(3000);
        }
        #endregion

        #region button
        // 帮助
        private void btnHelper_Click(object sender, EventArgs e)
        {
            lbMsg.Name = "abc";
            //lbMsg.Visible = true;
            lbMsg.AutoSize = false;
            lbMsg.Size = pictureBox1.Size - new Size(20, 200);
            lbMsg.Location = new Point(10, 10);
            lbMsg.BackColor = Color.NavajoWhite;

            lbMsg.Text = @"1. USB数据线连接安卓手机，并打开安卓手机的 USB 调试模式（打开方式请自动百度）。

2. 手动模式：右键 单击小人底部（即 起跳位置），左键单击目标位置。

3. 自动识别模式：

    1). 如果直接点击 跳 按钮，则会根据自动识别出来的位置跳。

    2). 如果识别不准确，可手动调整小人位置及目标位置，操作方法参考第2条。

4. 自动跳：该模式下自动识别、自动跳，不需要人工参与。

    1). 如果设置了跳跃次数，那么分多局游戏，每局游戏开始前，会自动计算一个随机数（n），每一局会比上一局多跳 n 次。

    2). 如果识别错误，或中间掉下，都会自动开始新的一局游戏，直到跳跃次数达到设置的值。

    3). 基本上，跳50次，能达到500分左右。



双击可关闭帮助窗口";

            lbMsg.Show();
        }

        private void lbMsg_DoubleClick(object sender, EventArgs e)
        {
            lbMsg.Hide();
        }

        // 自动识别
        private void chkRecognize_CheckedChanged(object sender, EventArgs e)
        {
            this.isAutoRecognize = this.chkRecognize.Checked;
            this.chkAutoJump.Enabled = this.chkRecognize.Checked;
            this.btnJump.Enabled = this.chkRecognize.Checked;

            if (!isAutoRecognize)
            {
                this.chkAutoJump.Checked = false;
                this.isAutoJump = false;
                this.btnJump.Enabled = false;
            }
        }

        // 自动跳
        private void chkJump_CheckedChanged(object sender, EventArgs e)
        {
            //this.isAutoJump = this.chkAutoJump.Checked;
            this.nJumpCount.Enabled = this.chkAutoJump.Checked;
            //this.btnJump.Enabled = !this.chkAutoJump.Checked;
        }

        // 手动跳
        private void btnJump_Click(object sender, EventArgs e)
        {
            if (this.chkAutoJump.Checked)
            {
                if (this.btnJump.Text == "跳")
                {
                    this.isAutoJump = this.chkAutoJump.Checked;
                    this.chkAutoJump.Enabled = false;
                    this.nJumpCount.Enabled = false;
                    this.max_jump_count = (int)this.nJumpCount.Value;

                    this.btnJump.Text = "停";

                    jump();
                }
                else
                {
                    this.isAutoJump = false;
                    this.chkAutoJump.Enabled = true;
                    this.nJumpCount.Enabled = true;

                    this.btnJump.Text = "跳";
                }
            }
            else
            {
                jump();
            }
        }

        // 
        private void nJumpCount_ValueChanged(object sender, EventArgs e)
        {
            //this.max_jump_count = (int)this.nJumpCount.Value;
        }
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
