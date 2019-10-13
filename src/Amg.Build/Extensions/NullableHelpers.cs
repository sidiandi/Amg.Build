using System;

namespace Amg.Build.Extensions
{
    public static class NullableHelpers
    {
        /// <summary>
        /// Transforms x into type Y when x is not null. Returns null otherwise.
        /// </summary>
        /// <typeparam name="Y"></typeparam>
        /// <typeparam name="X"></typeparam>
        /// <param name="x"></param>
        /// <param name="mapper"></param>
        /// <returns></returns>
        public static Y Map<Y, X>(this X? x, Func<X, Y> onNotNull, Func<Y> onNull) where X : class where Y : class
        {
            if (x == null)
            {
                return onNull();
            }
            else
            {
                return onNotNull(x);
            }
        }

        /// <summary>
        /// Transforms x into type Y when x is not null. Returns null otherwise.
        /// </summary>
        /// <typeparam name="Y"></typeparam>
        /// <typeparam name="X"></typeparam>
        /// <param name="x"></param>
        /// <param name="mapper"></param>
        /// <returns></returns>
        public static Y? Map<Y, X>(this X? x, Func<X, Y?> onNotNull) where X : class where Y : class
        {
            if (x == null)
            {
                return default;
            }
            else
            {
                return onNotNull(x);
            }
        }

        /// <summary>
        /// Executes an action when x is not null.
        /// </summary>
        /// <typeparam name="Y"></typeparam>
        /// <typeparam name="X"></typeparam>
        /// <param name="x"></param>
        /// <param name="mapper"></param>
        /// <returns></returns>
        public static X? Map<X>(this X? x, Action<X>? onNotNull, Action? onNull = null) where X : class
        {
            if (x == null)
            {
                if (onNull != null)
                {
                    onNull();
                }
                return x;
            }
            else
            {
                if (onNotNull != null)
                {
                    onNotNull(x);
                }
                return x;
            }
        }

    }
}
