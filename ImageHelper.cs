using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TiaoYiTiao
{
    public class ImageHelper
    {
        #region 字段
        PictureBox _pictureBox;

        // 小人的识别高度比
        const float characterHeightRate = 0.109f;
        //小人的识别宽度比
        const float characterWidthRate = 0.07f;

        /// <summary>
        /// 盒子扫描高度百分比
        /// </summary>
        const float targetHeightRate = 0.13f;

        // 小人的信息
        struct CharacterInfo
        {
            //public static Size @Size { get; set; }
            public static Color TopColor { get; set; }
            public static Color BottomColor { get; set; }
            public Point Top { get; set; }
            public Point Bottom { get; set; }
            public Point Left { get; set; }
            public Point Right { get; set; }
            public Point Center { get; set; }

            static CharacterInfo()
            {
                TopColor = Color.FromArgb(52, 53, 59);
                BottomColor = Color.FromArgb(54, 60, 102);
                //Left = Color.FromArgb(44,45,71);
                //Right = Color.FromArgb(56,53,80);
                //Size = new Size(76, 209);
            }
        }
        #endregion

        #region ctor
        public ImageHelper(PictureBox pictureBox)
        {
            this._pictureBox = pictureBox;
        }
        #endregion

        #region 自动识别
        /// <summary>
        /// 自动识别
        /// </summary>
        /// <param name="image"></param>
        public Point[] autoRecognize(Image image)
        {
            #region 定义变量
            Bitmap bitmap = new Bitmap(image);//, new Size(pw, ph)
            if (bitmap == null) return null;

            // 记录位置
            List<Point> characterLocation = new List<Point>();
            List<Point> targetLocation = new List<Point>();
            Color targetTopColor = new Color();

            // 起点位置 和 目标落点位置
            Point[] start_end_location = new Point[2];

            // 小人的信息
            CharacterInfo character = new CharacterInfo();

            // 窗口与手机屏幕的比率
            float rate = (float)(this._pictureBox.Width * 1.00 / image.Width * 1.00);

            //识别背景色
            Color bgColor = bitmap.GetPixel(0, image.Height / 2);

            Graphics g = Graphics.FromImage(image);
            #endregion

            #region 扫描区域：上下取中间1/3
            Point[] area = new Point[] {
                new Point (0, bitmap.Height * 1/3 ),
                new Point (bitmap.Width, bitmap.Height * 2/3)
            };
            // 画线
            g.DrawLine(new Pen(Color.Gray), 0, area[0].Y, image.Width, area[0].Y);
            g.DrawLine(new Pen(Color.Gray), 0, area[1].Y, image.Width, area[1].Y);
            #endregion

            #region 扫描图片
            Color currentColor;
            Point currentPoint;
            // 这一步操作比较耗时
            for (int y = area[0].Y; y < area[1].Y; y++)
            {
                for (int x = area[0].X; x < area[1].X; x++)
                {
                    currentColor = bitmap.GetPixel(x, y);
                    currentPoint = new Point(x, y);

                    // 忽略与背景色相似的色块
                    if (ColorHelper.CompareBaseRGB(bgColor, currentColor, 20)) continue;

                    #region 小人
                    // 是否与小人的颜色匹配
                    if (ColorHelper.CompareBaseRGB(currentColor, CharacterInfo.TopColor, 20) ||
                        ColorHelper.CompareBaseRGB(currentColor, CharacterInfo.BottomColor, 20))
                    {
                        characterLocation.Add(currentPoint);

                        // 给小人涂上颜色
                        g.FillEllipse(new SolidBrush(Color.Green), x, y, 2, 2);
                        Application.DoEvents();
                    }
                    #endregion

                    #region 目标盒子
                    // 搜索目标块的顶端像素
                    if (targetLocation.Count == 0)
                    {
                        if (ColorHelper.CompareBaseRGB(currentColor, bitmap.GetPixel(x, y + 2), 20)) // 排除有边框的可能性
                        {
                            if (!ColorHelper.CompareBaseRGB(CharacterInfo.TopColor, bitmap.GetPixel(x, y), 30) &&
                                !ColorHelper.CompareBaseRGB(CharacterInfo.TopColor, bitmap.GetPixel(x, y + 1), 30) &&
                                !ColorHelper.CompareBaseRGB(CharacterInfo.TopColor, bitmap.GetPixel(x, y + 3), 30))
                            {
                                targetTopColor = currentColor;

                                //g.FillEllipse(new SolidBrush(Color.Red), x, y, 2, 2);
                                targetLocation.Add(currentPoint);
                            }
                        }
                    }
                    // 给目标块涂色
                    if (targetLocation.Count > 0 && ColorHelper.CompareBaseRGB(currentColor, targetTopColor, 20) && currentPoint.Y <= targetLocation[0].Y + bitmap.Height * targetHeightRate)
                    {
                        targetLocation.Add(currentPoint);
                        g.FillEllipse(new SolidBrush(Color.Blue), x, y, 2, 2);
                        Application.DoEvents();
                    }
                    #endregion
                }
            }
            bitmap.Dispose();
            #endregion


            #region 画小人的外框
            {
                var width = (int)(image.Width * characterWidthRate);
                var height = (int)(image.Height * characterHeightRate);

                var top = characterLocation.Where(l => characterLocation.Contains(new Point(l.X, l.Y + height - 3))).OrderBy(l => l.Y).FirstOrDefault();
                var bottom = new Point(top.X, top.Y + height);
                var left = characterLocation.Where(l => 
                        l.X < top.X && 
                        l.X > top.X - width / 2 && 
                        l.Y > top.Y && l.Y < bottom.Y
                    ).OrderBy(l => l.X).FirstOrDefault();
                var right = new Point(left.X + width, left.Y);

                if (!(top.IsEmpty && bottom.IsEmpty && left.IsEmpty && right.IsEmpty))
                {
                    character.Top = top;
                    character.Bottom = bottom;
                    character.Left = left;
                    character.Right = right;

                    character.Center = new Point(left.X + (right.X - left.X) / 2, left.Y);
                    // 画边框
                    g.DrawRectangle(new Pen(Color.Red, 3), left.X, top.Y, right.X - left.X, bottom.Y - top.Y);
                    // 在中心画上一个点
                    g.FillEllipse(new SolidBrush(Color.Red), character.Center.X - 5, character.Center.Y - 5, 11, 11);
                }
            }
            #endregion

            #region 画目标块的边框
            {
                var top = targetLocation.OrderBy(t => t.Y).FirstOrDefault();
                var bottom = targetLocation.Where(t => t.X == top.X).OrderByDescending(t => t.Y).FirstOrDefault();

                // 计算顶点与底部的中心点
                var center = new Point(top.X, top.Y + (bottom.Y - top.Y) / 2);
                var left = targetLocation.Where(t => t.Y == center.Y).OrderBy(t => t.X).FirstOrDefault();
                var right = targetLocation.Where(t => t.Y == center.Y).OrderByDescending(t => t.X).FirstOrDefault();


                // 画边框
                g.DrawRectangle(new Pen(Color.Red, 3), left.X, top.Y, right.X - left.X, bottom.Y - top.Y);
                // 画中心点
                g.FillEllipse(new SolidBrush(Color.Red), center.X - 5, center.Y - 5, 11, 11);

                // 画小人与中心点的线
                if (!character.Center.IsEmpty)
                    g.DrawLine(new Pen(Color.Red), character.Center.X, character.Center.Y, center.X, center.Y);

                // 设置起跳位置
                start_end_location[0] = new Point((int)(rate * character.Center.X), (int)(rate * character.Center.Y));
                start_end_location[1] = new Point((int)(rate * center.X), (int)(rate * center.Y));
            }
            #endregion

            //toolStripStatusLabel2.Text = string.Format("起跳：{0}，目标：{1}", _start, _end);

            g.Dispose();
            GC.Collect();

            return start_end_location;
        }
        #endregion
    }
}
