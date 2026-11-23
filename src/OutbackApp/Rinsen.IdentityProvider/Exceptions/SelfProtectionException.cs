using System;

namespace Rinsen.IdentityProvider.Exceptions
{
    public class SelfProtectionException : Exception
    {
        public SelfProtectionException(string? message) : base(message)
        {
        }
    }
}
