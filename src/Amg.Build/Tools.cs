﻿using System;

namespace Amg.Build
{
    /// <summary>
    /// Frequently used tools
    /// </summary>
    public class Tools
    {
        /// <summary>
        /// Default tool settings
        /// </summary>
        public static ITool Default { get; set; } = new Tool()
            .WithOnOutput(old => (r, line) => Console.Out.WriteLine($"{r}:{line}"))
            .WithOnError(old => (r, line) => Console.Error.WriteLine($"{r}:{line}"));

        /// <summary>
        /// cmd.exe /c
        /// </summary>
        public static ITool Cmd => Default.WithFileName("cmd.exe").WithArguments("/c");
    }
}