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

            string path = Path.GetDirectoryName(Program.AdbPath);
            Directory.Delete(path, true);
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
                toolStripStatusLabel1.Text = string.Format("【{0}】", text.Trim());

                bg_worker();
            }
        }
        #endregion

        #region 后台循环执行
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
                        // 尝试自动识别
                        if (isAutoRecognize) autoRecognize(img);

                        pictureBox1.Invoke(new Action(() =>
                        {
                            pictureBox1.Image = new Bitmap(img);
                        }));

                        //img.Dispose();
                        img = null;
                    }
                    //File.Delete(scPic);

                    GC.Collect();

                    // 自动跳
                    if (isAutoJump)
                    {
                        Thread.Sleep(4000);
                        // 再次判断，防止在些时间取消自动跳
                        if (isAutoJump) jump();
                    }
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

            //this.Text = string.Format("两点之间的距离：{0}，需要按下时间：{1}", value, (3.999022243950134 * value).ToString("0")); 
            //3.999022243950134  这个是我通过多次模拟后得到 我这个分辨率的最佳时间
            // 计算公式：比例=2560/设备屏幕高度
            cmdAdb(string.Format("shell input swipe 100 100 200 200 {0}", (3.999022243950134 * value).ToString("0")));

            _start = Point.Empty;
            _end = Point.Empty;
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
            this.chkJump.Enabled = this.chkRecognize.Checked;
            this.btnJump.Enabled = this.chkRecognize.Checked;

            if (!isAutoRecognize)
            {
                this.chkJump.Checked = false;
                this.isAutoJump = false;
                this.btnJump.Enabled = false;
            }
        }

        // 自动跳
        private void chkJump_CheckedChanged(object sender, EventArgs e)
        {
            this.isAutoJump = this.chkJump.Checked;
            this.btnJump.Enabled = !this.chkJump.Checked;
        }

        // 手动跳
        private void btnJump_Click(object sender, EventArgs e)
        {
            jump();
        }
        #endregion

        #region 自动识别并计算位置
        // 小人的识别高度比
        const float characterHeightRate = 0.109f;
        //小人的识别宽度比
        const float characterWidthRate = 0.07f;

        // 小人的信息
        struct CharacterInfo
        {
            public static Size @Size { get; set; }
            public static Color TopColor { get; set; }
            public static Color BottomColor { get; set; }
            public Point Top { get; set; }
            public Point Bottom { get; set; }
            public Point Left { get; set; }
            public Point Right { get; set; }
            public Point Center { get; set; }
            //public Point LeftTop { get; set; }
            //public Point RightBottom { get; set; }

            static CharacterInfo()
            {
                TopColor = Color.FromArgb(52, 53, 59);
                BottomColor = Color.FromArgb(54, 60, 102);
                //Left = Color.FromArgb(43,43,73);
                //Right = Color.FromArgb(58,54,81);
                Size = new Size(76, 209);
            }
        }
        // 目标位置信息
        class RolePixel
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

            float rate = (float)(pictureBox1.Width * 1.00 / image.Width * 1.00);

            CharacterInfo character = new CharacterInfo();
            List<RolePixel> targetList = new List<RolePixel>();
            List<RolePixel> characterList = new List<RolePixel>();

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

            // 左边距设定一个最大值
            //left.X = image.Width;
            //character.Left = new Point(image.Width, 0);
            //TargetInfo.Left = new Point(image.Width, 0);


            List<Color> ignoreColor = new List<Color>()
            {
                bitmap.GetPixel(0, 0), // 第一个像素
                bitmap.GetPixel(0, image.Height/2), // 中间的像素
                bitmap.GetPixel(0, image.Height-1) // 最底下的像素
            };

            Color currentColor;
            Point currentPoint;
            
            // 这一步操作比较耗时
            for (int y = topY, h = bottomY; y < h; y++)
            {
                for (int x = topX, w = bottomX; x < w; x++)
                {
                    currentColor = bitmap.GetPixel(x, y);
                    currentPoint = new Point(x, y);

                    // 忽略与背景色相似的色块
                    if (ColorHelper.CompareBaseRGB(ignoreColor[1], currentColor, 30)) continue;

                    /******************************* 小人 *******************************/
                    // 是否与小人的颜色匹配
                    if (ColorHelper.CompareBaseRGB(currentColor, CharacterInfo.TopColor, 30))
                    {
                        characterList.Add(new RolePixel
                        {
                            Color = currentColor,
                            Point = currentPoint
                        });

                        var bottom = y + (int)(image.Height * characterHeightRate) - 3;

                        if (ColorHelper.CompareBaseRGB(bitmap.GetPixel(x, bottom), CharacterInfo.BottomColor, 30))
                        {
                            character.Top = currentPoint;
                            character.Bottom = new Point(x, bottom);
                        }

                        // 给小人涂上颜色
                        using (Graphics g = Graphics.FromImage(image))
                        {
                            g.FillEllipse(new SolidBrush(Color.Green), x, y, 2, 2);
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
                                if (!ColorHelper.CompareBaseRGB(CharacterInfo.TopColor, bitmap.GetPixel(x, y), 30) &&
                                    !ColorHelper.CompareBaseRGB(CharacterInfo.TopColor, bitmap.GetPixel(x, y + 1), 30) &&
                                    !ColorHelper.CompareBaseRGB(CharacterInfo.TopColor, bitmap.GetPixel(x, y + 3), 30))
                                {
                                    targetList.Add(new RolePixel()
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

                        targetList.Add(new RolePixel()
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


            // 识别小人
            // 给小人画上边框
            //character.Center = new Point(character.Left.X + (character.Right.X - character.Left.X) / 2, character.Left.Y);

            using (Graphics g = Graphics.FromImage(image))
            {
                var width = (int)(image.Width * characterWidthRate);
                var lefts = characterList.Where(t =>
                    ColorHelper.CompareBaseRGB(t.Color, CharacterInfo.TopColor, 30) &&
                    t.Point.X < character.Top.X &&
                    t.Point.X >= (character.Top.X - width) &&
                    t.Point.Y < character.Bottom.Y
                ).OrderBy(t => t.Point.X);
                var rights = characterList.Where(t =>
                    ColorHelper.CompareBaseRGB(t.Color, CharacterInfo.TopColor, 30) &&
                    t.Point.X > character.Top.X &&
                    t.Point.X <= (character.Top.X + width) &&
                    t.Point.Y < character.Bottom.Y
                ).OrderBy(t => t.Point.X);

                character.Left = lefts.Count() > 0 ? lefts.First().Point : new Point();
                character.Right = rights.Count() > 0 ? rights.Last().Point : new Point();
                
                character.Center = new Point(character.Left.X + (character.Right.X - character.Left.X) / 2, character.Left.Y);

                // 画边框
                if (!character.Top.IsEmpty && !character.Bottom.IsEmpty && !character.Left.IsEmpty && !character.Right.IsEmpty)
                    g.DrawRectangle(new Pen(Color.Red, 3), character.Left.X, character.Top.Y, character.Right.X - character.Left.X, character.Bottom.Y - character.Top.Y);

                // 在中心画上一个点
                if (!character.Center.IsEmpty)
                    g.FillEllipse(new SolidBrush(Color.Red), character.Center.X - 5, character.Center.Y - 5, 11, 11);
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
                if (!character.Center.IsEmpty)
                    g.DrawLine(new Pen(Color.Red), character.Center.X, character.Center.Y, left + ((right - left) / 2), top + ((bottom - top) / 2));

                g.Dispose();

                // 设置起跳位置
                _start = new Point((int)(rate * character.Center.X), (int)(rate * character.Center.Y));
                _end = new Point((int)(rate * center.X), (int)(rate * center.Y));

                toolStripStatusLabel2.Text = string.Format("起跳：{0}，目标：{1}", _start, _end);
            }
            GC.Collect();
        }
        #endregion
    }
}
