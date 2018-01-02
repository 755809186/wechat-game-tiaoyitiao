using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Numerics;

namespace TiaoYiTiao
{
    /// <summary>
    /// 颜色辅助类
    /// </summary>
    public class ColorHelper
    {
        /// <summary>
        /// 返回颜色集合的平均颜色
        /// </summary>
        public static Color GetAverageColor(IEnumerable<Color> colors)
        {
            var a = colors.Sum(c => c.A) / colors.Count();
            var r = colors.Sum(c => c.R) / colors.Count();
            var g = colors.Sum(c => c.G) / colors.Count();
            var b = colors.Sum(c => c.B) / colors.Count();

            return Color.FromArgb(a, r, g, b);
        }
        /// <summary>
        /// 返回两个颜色的平均颜色
        /// </summary>
        public static Color GetAverageColor(Color color1, Color color2)
        {
            var a = ((int)color1.A + (int)color2.A) / 2;
            var r = ((int)color1.R + (int)color2.R) / 2;
            var g = ((int)color1.G + (int)color2.G) / 2;
            var b = ((int)color1.B + (int)color2.B) / 2;

            return Color.FromArgb(a, r, g, b);
        }
        /// <summary>
        /// 返回两个颜色的相似度
        /// </summary>
        public static float GetColorSimilarity(Color color1, Color color2)
        {
            float result = 0;
            Vector3 vec1 = new Vector3(color1.R, color1.G, color1.B);
            Vector3 vec2 = new Vector3(color2.R, color2.G, color2.B);
            result = 1 / (1 + (vec1 - vec2).LengthSquared());
            return result;
        }
        /// <summary> 
        /// 基于RGB判断两个颜色是否相似
        /// </summary> 
        public static Boolean CompareBaseRGB(Color color1, Color color2, float distance = 30)
        {
            int r = (int)color1.R - (int)color2.R;
            int g = (int)color1.G - (int)color2.G;
            int b = (int)color1.B - (int)color2.B;
            int temp = (int)Math.Sqrt(r * r + g * g + b * b);

            return temp < distance;
        }
        /// <summary> 
        /// 基于HSB判断两个颜色是否相似
        /// </summary> 
        public static Boolean CompareBaseHSB(Color color1, Color color2, float distance)
        {
            //向量距离
            //float h = (Color1.GetHue - Color2.GetHue) / 360
            //float s = Color1.GetSaturation - Color2.GetSaturation
            //float b = Color1.GetBrightness - Color2.GetBrightness
            //float absDis = Math.Sqrt(h * h + s * s + b * b)
            //If absDis < Distance Then
            //    return True;
            //Else
            //    return False;
            //End If
            //向量夹角
            float h1 = color1.GetHue() / 360;
            float s1 = color1.GetSaturation();
            float b1 = color1.GetBrightness();
            float h2 = color2.GetHue() / 360;
            float s2 = color2.GetSaturation();
            float b2 = color2.GetBrightness();
            float absDis = (float)((h1 * h2 + s1 * s2 + b1 * b2) / (Math.Sqrt(h1 * h1 + s1 * s1 + b1 * b1) * Math.Sqrt(h2 * h2 + s2 * s2 + b2 * b2)));

            return absDis > distance / 5 + 0.8;

        }
        /// <summary> 
        /// 返回指定颜色的灰度值
        /// </summary> 
        public static int GetGrayOfColor(Color color)
        {
            int r, g, b;
            r = color.R;
            g = color.G;
            b = color.B;
            return (r + g + b) / 3;
        }

        /// <summary>
        /// 返回指定像素数据的块变换
        /// </summary>
        public static PixelData GetLumpPixelData(PixelData pixels, int range = 10)
        {
            Color[,] colors = pixels.GetColorsClone();

            for (var i = 0; i < pixels.Width; i++)
            {
                for (var j = 0; j < pixels.Height; j++)
                {
                    var r = (colors[i, j].R / range) * range;
                    var g = (colors[i, j].G / range) * range;
                    var b = (colors[i, j].B / range) * range;
                    colors[i, j] = Color.FromArgb(r, g, b);
                }
            }
            return PixelData.CreateFromColors(colors);
        }
        /// <summary>
        /// 返回指定像素数据的二值化变换
        /// </summary>
        public static PixelData GetThresholdPixelData(PixelData pixels, float threshold, Boolean isHSB = false)
        {
            Color[,] colors = pixels.GetColorsClone();

            Func<Color, bool> IsOverThreshold = (color) =>
            {
                return isHSB ?
                    (color.GetHue() / 360 + color.GetBrightness() + color.GetSaturation()) / 3 < threshold :
                    GetGrayOfColor(color) < threshold;
            };

            for (var i = 0; i < pixels.Width; i++)
            {
                for (var j = 0; j < pixels.Height; j++)
                {
                    colors[i, j] = IsOverThreshold(colors[i, j]) ? Color.Black : Color.White;
                }
            }
            return PixelData.CreateFromColors(colors);
        }
        /// <summary>
        /// 返回指定位图的轮廓图像
        /// </summary>
        public static PixelData GetOutLinePixelData(PixelData pixels, float distance, Boolean isHSB = false)
        {
            int[] OffsetX = { 0, 1, 0, -1 };
            int[] OffsetY = { -1, 0, 1, 0 };

            Func<Color, Color, float, bool> CompareColor = (c1, c2, d) =>
            {
                return isHSB ? CompareBaseHSB(c1, c2, d) : CompareBaseRGB(c1, c2, d);
            };

            Func<Color, Color, bool> CompareColorExtra = (c1, c2) =>
            {
                return isHSB ? c1.GetBrightness() - c2.GetBrightness() > 0 : GetGrayOfColor(c1) - GetGrayOfColor(c2) > 0;
            };

            Color[,] colors = pixels.GetColorsClone();
            Color color1, color2;

            for (var i = 1; i < pixels.Width - 1; i++)
            {
                for (var j = 1; j < pixels.Height - 1; j++)
                {
                    color1 = pixels.Colors[i, j];
                    for (var p = 0; p <= 3; p++)
                    {
                        color2 = pixels.Colors[i + OffsetX[p], j + OffsetY[p]];
                        // If Not CompareColor(color1, color2, distance) AndAlso CompareColorExtra(color1, color2) Then
                        if (!CompareColor(color1, color2, distance) && CompareColorExtra(color1, color2))
                            colors[i, j] = Color.Black;
                        else
                            colors[i, j] = Color.White;

                    }
                }
            }
            return PixelData.CreateFromColors(colors);
        }
        /// <summary>
        /// 返回指定二值化像素数据的空心变换
        /// </summary>
        public static PixelData GetHollowPixelData(PixelData pixels)
        {
            var colors = pixels.GetColorsClone();
            int[,] bools = GetPixelDataBools(pixels);
            for (var i = 0; i < pixels.Width; i++)
            {
                for (var j = 0; j < pixels.Height; j++)
                {
                    if (bools[i, j] == 1 && !IsBesieged(bools, i, j))
                        colors[i, j] = Color.Black;
                    else
                        colors[i, j] = Color.White;
                }
            }
            return PixelData.CreateFromColors(colors);
        }
        /// <summary>
        /// 返回指定二值化像素数据的反相变换
        /// </summary>
        public static PixelData GetInvertPixelData(PixelData pixels)
        {
            var colors = pixels.GetColorsClone();
            int[,] bools = GetPixelDataBools(pixels);
            for (var i = 0; i < pixels.Width; i++)
            {
                for (var j = 0; j < pixels.Height; j++)
                {
                    if (bools[i, j] == 0)
                        colors[i, j] = Color.Black;
                    else
                        colors[i, j] = Color.White;
                }
            }
            return PixelData.CreateFromColors(colors);
        }
        /// <summary>
        /// 返回指定二值化像素数据的的布尔数组
        /// </summary>
        public static int[,] GetPixelDataBools(PixelData pixels)
        {

            int[,] result = new int[pixels.Width - 1, pixels.Height - 1];
            Color[,] colors = pixels.GetColorsClone();
            for (var i = 0; i < pixels.Width; i++)
            {
                for (var j = 0; j < pixels.Height; j++)
                {
                    if (colors[i, j].Equals(Color.FromArgb(255, 255, 255)))
                        result[i, j] = 0;
                    else
                        result[i, j] = 1;
                }
            }
            return result;
        }
        /// <summary>
        /// 返回指定索引位置是否被包围
        /// </summary>
        private static bool IsBesieged(int[,] bools, int x, int y)
        {
            int[] OffsetX = { 0, 1, 0, -1 };
            int[] OffsetY = { -1, 0, 1, 0 };
            for (var i = 0; i < 3; i++)
            {
                int dx = x + OffsetX[i];
                int dy = y + OffsetY[i];
                if (IsIndexOverFlow(bools, dx, dy) || bools[dx, dy] == 0)
                    return false;
            }
            return true;
        }
        /// <summary>
        /// 返回指定数组索引是否越界
        /// </summary>
        private static bool IsIndexOverFlow(Array array, int x, int y)
        {
            int w = array.GetUpperBound(0);
            int h = array.GetUpperBound(1);
            return !(x >= 0 && y >= 0 && x <= w && y <= h);
        }
    }
}