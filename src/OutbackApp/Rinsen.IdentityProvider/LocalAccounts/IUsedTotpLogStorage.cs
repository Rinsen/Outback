using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Rinsen.IdentityProvider.LocalAccounts;

public interface IUsedTotpLogStorage
{
    Task CreateLog(UsedTotpLog usedTotp);

    Task<bool> HasBeenUsedAsync(Guid identityId, string code);

    Task PruneStaleAsync(TimeSpan maxAge);

}
