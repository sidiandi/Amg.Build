using System;

namespace Amg.CommandLine
{
    [AttributeUsage(AttributeTargets.Property)]
    internal class ShortAttribute : System.Attribute
    {
        public ShortAttribute(char name)
        {
            Name = name;
        }

        public char Name { get; private set; }
    }
}
