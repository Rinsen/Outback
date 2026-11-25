using System;

namespace Rinsen.IdentityProvider;

public class TotpCodeAlreadyUsedException : Exception
{
    public TotpCodeAlreadyUsedException(string message)
        : base(message)
    {

    }

    public TotpCodeAlreadyUsedException(string message, Exception innerException)
        : base(message, innerException)
    {

    }
}
