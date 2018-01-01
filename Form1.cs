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
                //计算两点直接的距离
                double value = Math.Sqrt(Math.Abs(_start.X - _end.X) * Math.Abs(_start.X - _end.X) + Math.Abs(_start.Y - _end.Y) * Math.Abs(_start.Y - _end.Y));

                _start = Point.Empty;

                //this.Text = string.Format("两点之间的距离：{0}，需要按下时间：{1}", value, (3.999022243950134 * value).ToString("0")); 
                //3.999022243950134  这个是我通过多次模拟后得到 我这个分辨率的最佳时间
                cmdAdb(string.Format("shell input swipe 100 100 200 200 {0}", (3.999022243950134 * value).ToString("0")));
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
                    }
                    File.Delete(scPic);

                    GC.Collect();
                    Thread.Sleep(1000);
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
        }
        #endregion

        #region 自动识别并计算位置
        // 小人尺寸：W:76, H:209
        // 小人的颜色范围
        struct RoleInfo
        {
            public static Color Top { get; private set; }
            public static Color Bottom { get; private set; }
            public static Color Left { get; private set; }
            public static Color Right { get; private set; }
            public static int Width { get; private set; }
            public static int Height { get; private set; }

            static RoleInfo()
            {
                Top = Color.FromArgb(52, 52, 59);
                Bottom = Color.FromArgb(54, 60, 102);
                Left = Color.FromArgb(43, 43, 73);
                Right = Color.FromArgb(58, 54, 81);
                Width = 76;
                Height = 209;
            }
        }

        /// <summary>
        /// 自动识别
        /// </summary>
        /// <param name="image"></param>
        private void autoRecognize(Image image)
        {
            //var image_rect = new Rectangle(0, 0, image.Width, image.Height);
            //var pw = image.Width;// pictureBox1.Width;
            //var ph = image.Height;// pictureBox1.Height;

            Bitmap bitmap = new Bitmap(image);//, new Size(pw, ph)

            //per = Math.Min(1, per);
            //per = Math.Max(0, per);
            Point top, right, bottom, left;
            top = right = bottom = left = new Point();
            // 左边距设定一个最大值
            left.X = image.Width;
            List<Color> ignoreColor = new List<Color>()
            {
                bitmap.GetPixel(0, 0), // 第一个像素
                bitmap.GetPixel(0, image.Height/2), // 中间的像素
                bitmap.GetPixel(0, image.Height-1) // 最底下的像素
            };

            Color color;

            // 这一步操作比较耗时
            for (int y = 0, h = image.Height; y < h; y++)
            {
                for (int x = 0, w = image.Width; x < w; x++)
                {
                    color = bitmap.GetPixel(x, y);
                    
                    //if (isSimilarColor(ignoreColor[0], color, 255)) continue;
                    if (isSimilarColor(ignoreColor[1], color, 255)) continue;

                    if (isSimilarColor(color, RoleInfo.Top) && top.IsEmpty)
                        top = new Point(x, y);
                    else if (isSimilarColor(color, RoleInfo.Bottom) && y > bottom.Y)
                        bottom = new Point(x, y);
                    else if (isSimilarColor(color, RoleInfo.Left) && x < left.X && y > left.Y)
                        left = new Point(x, y);
                    else if (isSimilarColor(color, RoleInfo.Right) && x > right.X && y > right.Y)
                        right = new Point(x, y);
                }
            }
            
            var location = new Point(left.X + (right.X - left.X) / 2, left.Y);
            float rate = (float)(pictureBox1.Width * 1.00 / image.Width * 1.00);
            _start = new Point((int)(rate * location.X), (int)(rate * location.Y));

            bitmap.Dispose();
            using (Graphics g = Graphics.FromImage(image))// pictureBox1.CreateGraphics())
            {
                // 画边框
                g.DrawRectangle(new Pen(Color.Red, 3), left.X, top.Y, right.X - left.X, bottom.Y - top.Y);
                // 在中心画上一个点
                g.FillEllipse(new SolidBrush(Color.Red), location.X - 2, location.Y - 2, 5, 5);
                //g.Save();
                g.Dispose();
                //image.Save("2.png");
            }
            GC.Collect();
        }

        // 计算是否是近似颜色
        private bool isSimilarColor(Color c1, Color c2, int offset=100) // 颜色近似值
        {
            int tmp = (int)(Math.Pow((c1.R - c2.R), 2) + Math.Pow((c1.G - c2.G), 2) + Math.Pow((c1.B - c2.B), 2));
            return Math.Abs(tmp - offset) <= offset;
        }
        #endregion
    }
}
