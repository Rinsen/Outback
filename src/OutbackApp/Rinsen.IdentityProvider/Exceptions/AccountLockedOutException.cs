using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rinsen.IdentityProvider.Exceptions
{
    /// <summary>
    /// SEC-016: thrown when a local account has exceeded the maximum number of
    /// failed login attempts and is locked out. The login path converts this to
    /// the same generic failure as an invalid password, so a locked account is
    /// indistinguishable from a wrong-password attempt.
    /// </summary>
    internal class AccountLockedOutException : Exception
    {
        public AccountLockedOutException(string? message) : base(message)
        {
        }
    }
}
