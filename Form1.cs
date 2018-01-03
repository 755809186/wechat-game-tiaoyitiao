using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
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
                if (_start.IsEmpty)
                {
                    MessageBox.Show("右健点击起始位置，左键点击结束位置");
                    return;
                }
                _end = ((MouseEventArgs)(e)).Location;

                this.jump();
            }
            else if (me.Button == MouseButtons.Right)//按下右键键是黑人底部的坐标
            {
                _start = ((MouseEventArgs)(e)).Location;
                //toolStripStatusLabel1.Text = "右健点击起始位置，左键点击结束位置";
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
                toolStripStatusLabel1.Text = string.Format("【{0}】右健点击起始位置，左键点击结束位置", text.Trim());

                bg_worker();
            }
        }
        #endregion

        #region 后台执行截屏
        void bg_worker()
        {
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
                        // 尝试自动识别计算小人位置
                        if (isAutoRecognize)
                            autoRecognize(img);

                        pictureBox1.Invoke(new Action(() =>
                        {
                            pictureBox1.Image = new Bitmap(img);
                        }));

                        //img.Dispose();
                        img = null;

                        if (isAutoJump)
                            jump();
                    }
                    File.Delete(scPic);

                    GC.Collect();
                    Thread.Sleep(3000);
                }
                if (img != null)
                    img.Dispose();
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
            //计算两点直接的距离
            double value = Math.Sqrt(Math.Abs(_start.X - _end.X) * Math.Abs(_start.X - _end.X) + Math.Abs(_start.Y - _end.Y) * Math.Abs(_start.Y - _end.Y));

            _start = Point.Empty;
            _end = Point.Empty;

            //this.Text = string.Format("两点之间的距离：{0}，需要按下时间：{1}", value, (3.999022243950134 * value).ToString("0")); 
            //3.999022243950134  这个是我通过多次模拟后得到 我这个分辨率的最佳时间
            // 计算公式：比例=2560/设备屏幕高度
            cmdAdb(string.Format("shell input swipe 100 100 200 200 {0}", (3.999022243950134 * value).ToString("0")));
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

        // 自动识别
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            this.isAutoRecognize = this.checkBox1.Checked;
            this.checkBox2.Enabled = this.checkBox1.Checked;

            if (!isAutoRecognize)
            {
                this.checkBox2.Checked = false;
                this.isAutoJump = false;
            }
        }

        // 自动跳
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            this.isAutoJump = this.checkBox2.Checked;
        }
        #endregion

        #region 自动识别并计算位置
        // 小人的信息
        struct RoleInfo
        {
            public static Color TopColor { get; set; }
            public static Size @Size { get; set; }
            public Point Top { get; set; }
            public Point Left { get; set; }
            public Point Right { get; set; }
            public Point Bottom { get; set; }
            public Point Center { get; set; }

            static RoleInfo()
            {
                TopColor = Color.FromArgb(52, 52, 59);
                Size = new Size(76, 209);
            }
        }
        // 目标位置信息
        struct TargetPixel
        {
            public Color @Color { get; set; }
            public Point @Point { get; set; }
        }

        /// <summary>
        /// 自动识别
        /// </summary>
        /// <param name="image"></param>
        private void autoRecognize(Image image)
        {
            Bitmap bitmap = new Bitmap(image);//, new Size(pw, ph)

            RoleInfo role = new RoleInfo();
            List<TargetPixel> targetList = new List<TargetPixel>();

            // 扫描区域：上下取中间1/3
            int topX, topY, bottomX, bottomY;
            topX = 0;
            topY = image.Height * 1 / 3;
            bottomX = image.Width;
            bottomY = image.Height * 2 / 3;

            using (Graphics g = Graphics.FromImage(image))
            {
                g.DrawLine(new Pen(Color.Gray), 0, topY, image.Width, topY);
                g.DrawLine(new Pen(Color.Gray), 0, bottomY, image.Width, bottomY);
                g.Dispose();
            }

            //Point top, right, bottom, left;
            //top = right = bottom = left = new Point();

            // 目标顶点位置
            //Point targetTopPoint = new Point();
            //Color targetTopColor = new Color();

            // 左边距设定一个最大值
            //left.X = image.Width;
            role.Left = new Point(image.Width, 0);
            //TargetInfo.Left = new Point(image.Width, 0);


            List<Color> ignoreColor = new List<Color>()
            {
                bitmap.GetPixel(0, 0), // 第一个像素
                bitmap.GetPixel(0, image.Height/2), // 中间的像素
                bitmap.GetPixel(0, image.Height-1) // 最底下的像素
            };

            Color currentColor;

            Stopwatch st = new Stopwatch();
            st.Start();
            // 这一步操作比较耗时
            for (int y = topY, h = bottomY; y < h; y++)
            {
                for (int x = topX, w = bottomX; x < w; x++)
                {
                    currentColor = bitmap.GetPixel(x, y);

                    // 忽略与背景色相似的色块
                    //if (ColorHelper.CompareBaseRGB(ignoreColor[0], color, 30)) continue;
                    if (ColorHelper.CompareBaseRGB(ignoreColor[1], currentColor, 30)) continue;

                    /******************************* 小人 *******************************/
                    var isSimilarity = ColorHelper.CompareBaseRGB(currentColor, RoleInfo.TopColor, 30);

                    // 是否与小人的颜色匹配
                    if (isSimilarity)
                    {
                        // 获取小人的上下左右四个点的坐标
                        if (role.Top.IsEmpty)
                        {
                            role.Top = new Point(x, y);
                        }
                        else
                        {
                            if (Math.Abs(role.Top.X - x) <= RoleInfo.Size.Width && Math.Abs(role.Top.Y - y) <= RoleInfo.Size.Height)
                            {
                                if (y > role.Bottom.Y)
                                    role.Bottom = new Point(x, y);
                                else if (x < role.Left.X && y > role.Left.Y)
                                    role.Left = new Point(x, y);
                                else if (x > role.Right.X && y > role.Right.Y)
                                    role.Right = new Point(x, y);
                            }
                        }

                        // 给小人涂上颜色
                        using (Graphics g = Graphics.FromImage(image))
                        {
                            g.FillEllipse(new SolidBrush(Color.Blue), x, y, 2, 2);
                            g.Dispose();
                        }
                    }
                    

                    /******************************* 目标 *******************************/
                    // 搜索目标块的顶端像素
                    if (targetList.Count == 0)
                    {
                        if (!ColorHelper.CompareBaseRGB(currentColor, bitmap.GetPixel(x, y - 1), 10))
                        {
                            if (!ColorHelper.CompareBaseRGB(currentColor, bitmap.GetPixel(x - 1, y), 10))
                            {
                                if (!ColorHelper.CompareBaseRGB(RoleInfo.TopColor, bitmap.GetPixel(x, y), 30) &&
                                    !ColorHelper.CompareBaseRGB(RoleInfo.TopColor, bitmap.GetPixel(x, y + 1), 30) &&
                                    !ColorHelper.CompareBaseRGB(RoleInfo.TopColor, bitmap.GetPixel(x, y + 3), 30))
                                {
                                    targetList.Add(new TargetPixel()
                                    {
                                        Color = currentColor,
                                        Point = new Point(x, y)
                                    });
                                }
                            }
                        }
                    }

                    // 给目标块涂色
                    if(targetList.Count > 0 && ColorHelper.CompareBaseRGB(currentColor, targetList[0].Color, 10))
                    {
                        var last = targetList[targetList.Count - 1];
                        if (Math.Abs(x - last.Point.X) > 10 && Math.Abs(y - last.Point.Y) > 10) continue;

                        targetList.Add(new TargetPixel()
                        {
                            Color = currentColor,
                            Point = new Point(x, y)
                        });

                        using (Graphics g = Graphics.FromImage(image))
                        {
                            g.FillEllipse(new SolidBrush(Color.Blue), x, y, 2, 2);
                            g.Dispose();
                        }
                    }
                }
            }
            GC.Collect();
            bitmap.Dispose();

            st.Stop();
            var ems = st.ElapsedMilliseconds;

            // 给小人画上边框
            role.Center = new Point(role.Left.X + (role.Right.X - role.Left.X) / 2, role.Left.Y);
            float rate = (float)(pictureBox1.Width * 1.00 / image.Width * 1.00);

            using (Graphics g = Graphics.FromImage(image))// pictureBox1.CreateGraphics())
            {
                // 画边框
                g.DrawRectangle(new Pen(Color.Red, 3), role.Left.X, role.Top.Y, role.Right.X - role.Left.X, role.Bottom.Y - role.Top.Y);
                // 在中心画上一个点
                g.FillEllipse(new SolidBrush(Color.Red), role.Center.X - 5, role.Center.Y - 5, 11, 11);
                //g.Save();
                g.Dispose();
                //image.Save("2.png");
            }
            GC.Collect();


            // 画目标块的边框
            using (Graphics g = Graphics.FromImage(image))
            {                
                var left = targetList.Min(t => t.Point.X);
                var top = targetList.Min(t => t.Point.Y);
                var right = targetList.Max(t => t.Point.X);
                var bottom = targetList.Max(t => t.Point.Y);
                var center = new Point(left + ((right - left) / 2), top + ((bottom - top) / 2));

                // 画边框
                g.DrawRectangle(new Pen(Color.Red, 3), left, top, right - left, bottom - top);
                // 画中心点
                g.FillEllipse(new SolidBrush(Color.Red), center.X - 5, center.Y - 5, 11, 11);

                // 画小人与中心点的线
                g.DrawLine(new Pen(Color.Red), role.Center.X, role.Center.Y, left + ((right - left) / 2), top + ((bottom - top) / 2));

                g.Dispose();

                // 设置起跳位置
                _start = new Point((int)(rate * role.Center.X), (int)(rate * role.Center.Y));
                _end = new Point((int)(rate * center.X), (int)(rate * center.Y));
            }
            GC.Collect();
        }
        #endregion
    }
}
