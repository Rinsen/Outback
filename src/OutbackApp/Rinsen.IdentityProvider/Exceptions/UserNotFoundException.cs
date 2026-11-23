using System;

namespace Rinsen.IdentityProvider.Exceptions
{
    public class UserNotFoundException : Exception
    {
        public UserNotFoundException(string? message) : base(message)
        {
        }
    }
}
