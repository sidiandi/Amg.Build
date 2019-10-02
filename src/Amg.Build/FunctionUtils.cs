﻿using System;
using System.Collections.Generic;

namespace Amg.Build
{
    /// <summary>
    /// Utilities for functional programming
    /// </summary>
    public static class FunctionUtils
    {
        /// <summary>
        /// Creates a function that executes f only once and caches the result 
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static Func<Input, Output> Once<Input, Output>(Func<Input, Output> f)
        {
            var resultCache = new Dictionary<Input, Output>();
            return new Func<Input, Output>((input) =>
            {
                return resultCache.GetOrAdd(input, () =>
                {
                    return f(input);
                });
            });
        }
    }
}
