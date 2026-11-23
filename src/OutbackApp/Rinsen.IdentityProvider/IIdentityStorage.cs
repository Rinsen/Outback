using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rinsen.IdentityProvider;

public interface IIdentityStorage
{
    Task CreateAsync(Identity identity);
    Task<Identity> GetAsync(Guid identityId);
    Task<Identity?> TryGetAsync(Guid identityId);
    Task<List<Identity>> GetAllAsync();
    Task UpdateAsync(Identity identity);
    Task DeleteAsync(Identity identity);
}
