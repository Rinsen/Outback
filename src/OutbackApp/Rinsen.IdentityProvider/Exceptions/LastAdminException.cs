using System;

namespace Rinsen.IdentityProvider.Exceptions
{
    public class LastAdminException : Exception
    {
        public LastAdminException(string? message) : base(message)
        {
        }
    }
}
