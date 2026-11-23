using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rinsen.IdentityProvider;

public interface IUserService
{
    Task<List<User>> GetAllAsync();
    Task<User> GetAsync(Guid identityId);
    Task<User> CreateAsync(string givenName, string surname, string email, string phoneNumber, string password);
    Task UpdateAsync(Guid identityId, string givenName, string surname, string email, string phoneNumber, bool isAdministrator, bool isDisabled);
    Task ResetPasswordAsync(Guid identityId, string newPassword);
    Task<string> EnableTotpAsync(Guid identityId);
    Task DisableTotpAsync(Guid identityId);
    Task DeleteAsync(Guid identityId);
}
