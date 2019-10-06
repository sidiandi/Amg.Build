using Cake.Core;
using Cake.Frosting;
using System;
using System.Reflection;

namespace Amg.Build.Cake
{
    /// <summary>
    /// Adapter for Cake (https://cakebuild.net/)
    /// </summary>
    public class Cake
    {
        /// <summary>
        /// Creates an ICakeContext that can be used to use all Cake addins (https://cakebuild.net/addins/).
        /// </summary>
        /// <returns></returns>
        public static ICakeContext CreateContext()
        {
            var h = new CakeHostBuilder().Build();
            var contextField = h.GetType().GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance);
            var cake = (ICakeContext)contextField.GetValue(h);
            return cake;
        }
    }
}
