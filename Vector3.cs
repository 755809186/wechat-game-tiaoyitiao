using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace TiaoYiTiao
{
#if NET40
    //[Conditional("NET40")]
    public class Vector3
    {
        public int x;
        public int y;
        public int z;

        public Vector3(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vector3 operator -(Vector3 A, Vector3 vtarger)
        {
            Vector3 v3 = Zero();
            v3.x = A.x - vtarger.x;
            v3.y = A.y - vtarger.y;
            v3.z = A.z = vtarger.z;
            return v3;
        }


        public static Vector3 operator +(Vector3 vtarger)
        {
            Vector3 v3 = Zero();
            //v3.x = x + vtarger.x;  
            //v3.y = this.y + vtarger.y;  
            //v3.z = this.z = vtarger.z;  
            return v3;
        }

        public static Vector3 operator *(Vector3 A, Vector3 B)
        {
            Vector3 v3 = Zero();
            v3.x = A.x + B.x;
            v3.y = A.y + B.y;
            v3.z = A.z + B.z;
            return v3;
        }
        /// <summary>  
        /// a = [a1,a2,a3]  b = [b1,b2,b3]  axb=[a2b3-a3b2,a3b1-a1b3,a1b2-a2b1]差乘  
        /// </summary>  
        /// <param name="vtarger"></param>  
        /// <returns></returns>  
        public Vector3 Cross(Vector3 vtarger)
        {
            Vector3 v3 = Zero();
            v3.x = this.y * vtarger.z - this.z * vtarger.y;
            v3.y = this.z * vtarger.x - this.x * vtarger.z;
            v3.z = this.x * vtarger.y - this.y * vtarger.x;
            return v3;
        }


        /// <summary>  
        /// 点积  
        /// </summary>  
        /// <param name="vtarger"></param>  
        /// <returns></returns>  
        public float Dot(Vector3 vtarger)
        {
            return this.x * vtarger.x + this.y * vtarger.y + this.z * vtarger.z;
        }

        public static Vector3 Zero()
        {
            Vector3 v3 = new Vector3(0, 0, 0);
            return v3;
        }

        public static Vector3 One()
        {
            Vector3 v3 = new Vector3(1, 1, 1);
            return v3;
        }

        public float LengthSquared()
        {
            //if (Vector.IsHardwareAccelerated)
            //{
            //    return Vector3.Dot(this, this);
            //}
            return this.x * this.x + this.y * this.y + this.z * this.z;
        }
    }
#endif
}
