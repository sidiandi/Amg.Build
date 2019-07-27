using System;
using System.Collections.Generic;
using System.Text;

namespace Amg.Build
{
    public class Nothing
    {
        public static readonly Nothing Instance = new Nothing();
        public override string ToString()
        {
            return "nothing";
        }
    }
}
