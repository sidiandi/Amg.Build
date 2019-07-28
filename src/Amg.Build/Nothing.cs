using System;
using System.Collections.Generic;
using System.Text;

namespace Amg.Build
{
    /// <summary>
    /// Use for "void" target input or output
    /// </summary>
    public class Nothing
    {
        /// <summary>
        /// Single instance of the Nothing class
        /// </summary>
        public static readonly Nothing Instance = new Nothing();

        /// <summary />
        public override string ToString()
        {
            return "nothing";
        }

        /// <summary />
        public override bool Equals(object obj)
        {
            return obj is Nothing;
        }

        /// <summary />
        public override int GetHashCode()
        {
            return 0;
        }

        private Nothing()
        {

        }
    }
}
