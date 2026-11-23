using Microsoft.Extensions.Logging;
using OtpNet;
using Rinsen.IdentityProvider.Exceptions;
using Rinsen.IdentityProvider.LocalAccounts;
using Rinsen.Outback.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Rinsen.IdentityProvider;

public class UserService : IUserService
{
    private readonly IIdentityStorage _identityStorage;
    private readonly ILocalAccountStorage _localAccountStorage;
    private readonly IIdentityAttributeStorage _identityAttributeStorage;
    private readonly IIdentityService _identityService;
    private readonly ILocalAccountService _localAccountService;
    private readonly IIdentityAccessor _identityAccessor;
    private readonly PasswordHashGenerator _passwordHashGenerator;
    private readonly IdentityOptions _options;
    private readonly ILogger<UserService> _log;

    private static readonly RandomNumberGenerator _cryptoRandom = RandomNumberGenerator.Create();

    public UserService(IIdentityStorage identityStorage,
        ILocalAccountStorage localAccountStorage,
        IIdentityAttributeStorage identityAttributeStorage,
        IIdentityService identityService,
        ILocalAccountService localAccountService,
        IIdentityAccessor identityAccessor,
        PasswordHashGenerator passwordHashGenerator,
        IdentityOptions options,
        ILogger<UserService> log)
    {
        _identityStorage = identityStorage;
        _localAccountStorage = localAccountStorage;
        _identityAttributeStorage = identityAttributeStorage;
        _identityService = identityService;
        _localAccountService = localAccountService;
        _identityAccessor = identityAccessor;
        _passwordHashGenerator = passwordHashGenerator;
        _options = options;
        _log = log;
    }

    public async Task<List<User>> GetAllAsync()
    {
        var identities = await _identityStorage.GetAllAsync();
        var localAccounts = (await _localAccountStorage.GetAllAsync()).ToDictionary(m => m.IdentityId);

        var users = new List<User>(identities.Count);
        foreach (var identity in identities)
        {
            localAccounts.TryGetValue(identity.IdentityId, out var localAccount);
            users.Add(await MapToUserAsync(identity, localAccount));
        }

        return users;
    }

    public async Task<User> GetAsync(Guid identityId)
    {
        var identity = await GetIdentityOrThrow(identityId);

        var localAccount = await TryGetLocalAccountAsync(identityId);

        return await MapToUserAsync(identity, localAccount);
    }

    public async Task<User> CreateAsync(string givenName, string surname, string email, string phoneNumber, string password)
    {
        // The local account's login id is the email address, matching the
        // registration flow in IdentityController.Create.
        var createIdentityResult = await _identityService.CreateAsync(givenName, surname, email, phoneNumber);

        if (!createIdentityResult.Succeeded || createIdentityResult.Identity is null)
        {
            throw new IdentityAlreadyExistException($"Identity {email} already exists", null);
        }

        var identity = createIdentityResult.Identity;

        try
        {
            var createLocalAccountResult = await _localAccountService.CreateAsync(identity.IdentityId, email, password);

            if (!createLocalAccountResult.Succeeded || createLocalAccountResult.LocalAccount is null)
            {
                throw new LocalAccountAlreadyExistException($"Login id {email} already exists", null);
            }
        }
        catch (LocalAccountAlreadyExistException)
        {
            // The identity was created but the local account could not be: roll
            // it back so no orphan identity is left behind.
            identity.Deleted = DateTimeOffset.Now;
            await _identityStorage.DeleteAsync(identity);
            throw;
        }

        _log.LogInformation("User {LoginId} created for identity {IdentityId}", email, identity.IdentityId);

        return await GetAsync(identity.IdentityId);
    }

    public async Task UpdateAsync(Guid identityId, string givenName, string surname, string email, string phoneNumber, bool isAdministrator, bool isDisabled)
    {
        var currentUserId = GetCurrentUserId();

        var identity = await GetIdentityOrThrow(identityId);

        var attributes = (await _identityAttributeStorage.GetIdentityAttributesAsync(identityId)).ToList();
        var adminAttribute = attributes.FirstOrDefault(m => m.Attribute == "Administrator");

        if (!isAdministrator && adminAttribute is not null)
        {
            if (identityId == currentUserId)
            {
                throw new SelfProtectionException("You cannot remove Administrator from your own account.");
            }

            var adminCount = await _identityAttributeStorage.CountAdministratorsAsync();
            if (adminCount <= 1)
            {
                throw new LastAdminException("This is the last administrator account; at least one administrator is required.");
            }
        }

        var localAccount = await TryGetLocalAccountAsync(identityId);

        if (isDisabled && localAccount is not null && !localAccount.IsDisabled && identityId == currentUserId)
        {
            throw new SelfProtectionException("You cannot disable your own account.");
        }

        identity.GivenName = givenName;
        identity.Surname = surname;
        identity.Email = email;
        identity.PhoneNumber = phoneNumber;

        await _identityStorage.UpdateAsync(identity);

        if (isAdministrator && adminAttribute is null)
        {
            await _identityAttributeStorage.CreateAsync(identity, "Administrator");
        }
        else if (!isAdministrator && adminAttribute is not null)
        {
            await _identityAttributeStorage.DeleteAsync(adminAttribute);
        }

        if (localAccount is not null && localAccount.IsDisabled != isDisabled)
        {
            localAccount.IsDisabled = isDisabled;
            await _localAccountStorage.UpdateAsync(localAccount);
        }

        _log.LogInformation("User {Email} (identity {IdentityId}) updated by {CurrentUserId}", identity.Email, identityId, currentUserId);
    }

    public async Task ResetPasswordAsync(Guid identityId, string newPassword)
    {
        await GetIdentityOrThrow(identityId);

        var localAccount = await GetLocalAccountOrThrow(identityId);

        var salt = new byte[_options.NumberOfBytesInPasswordSalt];
        _cryptoRandom.GetBytes(salt);

        localAccount.PasswordSalt = salt;
        localAccount.PasswordHash = _passwordHashGenerator.GetPasswordHash(salt, newPassword, localAccount.IterationCount, _options.NumberOfBytesInPasswordHash);
        localAccount.FailedLoginCount = 0;

        await _localAccountStorage.UpdateAsync(localAccount);

        _log.LogInformation("Password reset for local account {LoginId}", localAccount.LoginId);
    }

    public async Task<string> EnableTotpAsync(Guid identityId)
    {
        await GetIdentityOrThrow(identityId);

        var localAccount = await GetLocalAccountOrThrow(identityId);

        if (localAccount.SharedTotpSecret is null || localAccount.SharedTotpSecret.Length == 0)
        {
            var secret = new byte[64];
            _cryptoRandom.GetBytes(secret);
            localAccount.SharedTotpSecret = secret;
        }

        localAccount.TwoFactorTotpEnabled = DateTimeOffset.Now;

        await _localAccountStorage.UpdateAsync(localAccount);

        return Base32Encoding.ToString(localAccount.SharedTotpSecret);
    }

    public async Task DisableTotpAsync(Guid identityId)
    {
        await GetIdentityOrThrow(identityId);

        var localAccount = await GetLocalAccountOrThrow(identityId);

        localAccount.TwoFactorTotpEnabled = null;

        await _localAccountStorage.UpdateAsync(localAccount);
    }

    public async Task DeleteAsync(Guid identityId)
    {
        var currentUserId = GetCurrentUserId();

        var identity = await GetIdentityOrThrow(identityId);

        if (identityId == currentUserId)
        {
            throw new SelfProtectionException("You cannot delete your own account.");
        }

        var attributes = (await _identityAttributeStorage.GetIdentityAttributesAsync(identityId)).ToList();
        if (attributes.Any(m => m.Attribute == "Administrator"))
        {
            var adminCount = await _identityAttributeStorage.CountAdministratorsAsync();
            if (adminCount <= 1)
            {
                throw new LastAdminException("This is the last administrator account; it cannot be deleted.");
            }
        }

        identity.Deleted = DateTimeOffset.Now;
        await _identityStorage.DeleteAsync(identity);

        var localAccount = await TryGetLocalAccountAsync(identityId);
        if (localAccount is not null)
        {
            await _localAccountStorage.DeleteAsync(localAccount);
        }

        // Soft-delete the user's active attributes (e.g. Administrator) so
        // the deleted user does not keep holding them.
        foreach (var attribute in attributes)
        {
            await _identityAttributeStorage.DeleteAsync(attribute);
        }

        _log.LogInformation("User {Email} (identity {IdentityId}) deleted by {CurrentUserId}", identity.Email, identityId, currentUserId);
    }

    private async Task<Identity> GetIdentityOrThrow(Guid identityId)
    {
        var identity = await _identityStorage.TryGetAsync(identityId);

        // A soft-deleted user is treated as not found for every operation.
        if (identity is null || identity.Deleted is not null)
        {
            throw new UserNotFoundException($"User {identityId} not found");
        }

        return identity;
    }

    private Guid GetCurrentUserId()
    {
        // The login cookie carries the identity id in the "sub" claim
        // (see LoginService.GetClaimsForIdentityAsync).
        return _identityAccessor.ClaimsPrincipal.GetClaimGuidValue(m => m.Type == StandardClaims.Subject);
    }

    private async Task<LocalAccount?> TryGetLocalAccountAsync(Guid identityId)
    {
        try
        {
            return await _localAccountStorage.GetAsync(identityId);
        }
        catch (AccountNotFoundException)
        {
            return null;
        }
    }

    private async Task<LocalAccount> GetLocalAccountOrThrow(Guid identityId)
    {
        var localAccount = await TryGetLocalAccountAsync(identityId);

        if (localAccount is null)
        {
            throw new UserNotFoundException($"User {identityId} has no local account");
        }

        return localAccount;
    }

    private async Task<User> MapToUserAsync(Identity identity, LocalAccount? localAccount)
    {
        var attributes = await _identityAttributeStorage.GetIdentityAttributesAsync(identity.IdentityId);

        return new User
        {
            IdentityId = identity.IdentityId,
            GivenName = identity.GivenName,
            Surname = identity.Surname,
            Email = identity.Email,
            PhoneNumber = identity.PhoneNumber,
            LoginId = localAccount?.LoginId ?? string.Empty,
            HasLocalAccount = localAccount is not null,
            IsAdministrator = attributes.Any(m => m.Attribute == "Administrator"),
            TwoFactorTotpEnabled = localAccount?.TwoFactorTotpEnabled is not null,
            IsDisabled = localAccount?.IsDisabled ?? false,
            Created = identity.Created,
            Updated = identity.Updated
        };
    }
}
