using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Amg.Build
{
    class TestUtil
    {
        public class Output
        {
            public Output(string capturedOut, string capturedError)
            {
                Out = capturedOut;
                Error = capturedError;
            }

            public string Out { get; private set; }
            public string Error { get; private set; }
        }

        public static Output CaptureOutput(Action action)
        {
            var originalOut = System.Console.Out;
            var capturedOut = new StringWriter();
            Console.SetOut(capturedOut);
            var originalError = System.Console.Error;
            var capturedError = new StringWriter();
            Console.SetError(capturedError);

            try
            {
                action();
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);

                Console.WriteLine($@"Captured out:
{capturedOut.ToString()}");
                Console.WriteLine($@"Captured error:
{capturedError.ToString()}");
            }

            return new Output(capturedOut.ToString(), capturedError.ToString());
        }
    }
}
