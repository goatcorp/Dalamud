using System;

namespace Dalamud.Bootstrap.SqexArg
{    
    public class SqexArgException : Exception
    {
        public SqexArgException() { }
        public SqexArgException(string message) : base(message) { }
        public SqexArgException(string message, Exception inner) : base(message, inner) { }
    }
}
