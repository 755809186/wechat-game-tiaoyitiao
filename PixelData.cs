using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiaoYiTiao
{
    public class PixelData
    {
        /// <summary>
        /// 颜色数组
        /// </summary>
        public Color[,] Colors;

        /// <summary>
        /// 宽度
        /// </summary>
        public int Width;

        /// <summary>
        /// 高度
        /// </summary>
        public int Height;

        /// <summary>
        /// 创建并初始化一个对象
        /// </summary>
        public PixelData(int w, int h)
        {
            Colors = new Color[w - 1, h - 1];
            this.Width = w;
            this.Height = h;
        }

        /// <summary>
        /// 从指定的颜色数组创建像素数据
        /// </summary>
        public static PixelData CreateFromColors(Color[,] colors)
        {
            var w = colors.GetUpperBound(0) + 1;
            var h = colors.GetUpperBound(1) + 1;
            return new PixelData(w, h) { Colors = colors };
        }
        /// <summary>
        /// 返回颜色数组的浅表副本
        /// </summary>
        public Color[,] GetColorsClone()
        {
            return Colors.Clone() as Color[,];
        }
    }
}
