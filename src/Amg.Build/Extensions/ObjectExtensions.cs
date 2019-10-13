using System;
using System.Linq;

namespace Amg.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Writes instance properties
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static IWritable Destructure(this object x) => TextFormatExtensions.GetWritable(_ => _.Dump(x));

        /// <summary>
        /// object properties as table
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static IWritable PropertiesTable(this object x)
        {
            return x.GetType()
                .GetProperties()
                .Select(p => new { p.Name, Value = p.GetValue(x, new object[] { }) })
                .ToTable(header: false);
        }

        /// <summary>
        /// like ToString, but never throws. x can also be null.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static string SafeToString(this object x)
        {
            try
            {
                return x.ToString();
            }
            catch
            {
                return String.Empty;
            }
        }
    }
}
