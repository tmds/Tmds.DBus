using System;

namespace XunitSkip
{
    public class SkipTestException : Exception
    {
        public SkipTestException(string reason)
            : base(reason) { }
    }
}
