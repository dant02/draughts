using System;

namespace LogicLib
{
    /// <summary>
    /// Několik rozšiřujicích funkcí
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// sloupec z int
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static int GetCol(this int i) { return i % 10; }

        /// <summary>
        /// řádek z int
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static int GetRow(this int i) { return i - (i % 10); }

        /// <summary>
        /// proměna int na enum
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="i"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool ToEnum<T>(this int i, out T value)
        {
            value = default(T);
            try { value = (T)Enum.ToObject(typeof(T), i); }
            catch { return false; }
            //return true;
            return Enum.IsDefined(typeof(T), value);
        }
    }
}