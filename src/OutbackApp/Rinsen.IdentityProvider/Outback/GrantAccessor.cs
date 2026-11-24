using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Rinsen.IdentityProvider.Outback.Entities;
using Rinsen.Outback.Accessors;
using Rinsen.Outback.Grants;

namespace Rinsen.IdentityProvider.Outback;


public class GrantAccessor : IGrantAccessor
{
    private readonly OutbackDbContext _outbackDbContext;

    public GrantAccessor(OutbackDbContext outbackDbContext)
    {
        _outbackDbContext = outbackDbContext;
    }

    public async Task<AuthorizationCodeGrant> GetCodeGrantAsync(string code)
    {
        var outbackCodeGrant = await _outbackDbContext.CodeGrants.Include(m => m.Client).SingleOrDefaultAsync(m => m.Code == code && m.Resolved == null);

        if (outbackCodeGrant == default)
        {
            throw new Exception($"Code grant not found for code {code}");
        }

        if (outbackCodeGrant.Client is null)
        {
            throw new Exception($"No client found for code {code}");
        }

        outbackCodeGrant.Resolved = DateTimeOffset.Now;
        await _outbackDbContext.SaveChangesAsync();

        return new AuthorizationCodeGrant
        {
            ClientId = outbackCodeGrant.Client.ClientId,
            Code = outbackCodeGrant.Code,
            CodeChallenge = outbackCodeGrant.CodeChallenge,
            CodeChallengeMethod = outbackCodeGrant.CodeChallengeMethod,
            Created = outbackCodeGrant.Created,
            Expires = outbackCodeGrant.Expires,
            Nonce = outbackCodeGrant.Nonce,
            RedirectUri = outbackCodeGrant.RedirectUri,
            Scope = outbackCodeGrant.Scope,
            State = outbackCodeGrant.State,
            SubjectId = outbackCodeGrant.SubjectId.ToString(),
        };
    }

    public async Task<DeviceCodeGrant> GetDeviceAuthorizationGrantAsync(string deviceCode)
    {
        // Look up the grant regardless of its resolved state so that reuse of an
        // already-redeemed device code can be detected (RFC 8628 §3.2).
        var outbackDeviceAuthorizationGrant = await _outbackDbContext.DeviceAuthorizationGrants.Include(m => m.Client).SingleOrDefaultAsync(m => m.DeviceCode == deviceCode);

        if (outbackDeviceAuthorizationGrant == default)
        {
            throw new Exception("Device authorization grant not found");
        }

        if (outbackDeviceAuthorizationGrant.Client is null)
        {
            throw new Exception("No client found for device authorization grant");
        }

        return new DeviceCodeGrant
        {
            ClientId = outbackDeviceAuthorizationGrant.Client.ClientId,
            SubjectId = outbackDeviceAuthorizationGrant.SubjectId == Guid.Empty ? null : outbackDeviceAuthorizationGrant.SubjectId.ToString(),
            AccessIsRejected = outbackDeviceAuthorizationGrant.AccessIsRejected,
            DeviceCode = outbackDeviceAuthorizationGrant.DeviceCode,
            UserCode = outbackDeviceAuthorizationGrant.UserCode,
            Scope = outbackDeviceAuthorizationGrant.Scope,
            UserCodeExpiration = outbackDeviceAuthorizationGrant.UserCodeExpiration,
            Interval = outbackDeviceAuthorizationGrant.Interval,
            Resolved = outbackDeviceAuthorizationGrant.Resolved,
        };
    }

    public async Task<PersistedGrant> GetPersistedGrantAsync(string clientId, string subjectId)
    {
        var subjectIdGuid = Guid.Parse(subjectId);

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var outbackPersistedGrant = await _outbackDbContext.PersistedGrants.Include(m => m.Client).SingleOrDefaultAsync(m => m.Client.ClientId == clientId && m.SubjectId == subjectIdGuid);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        if (outbackPersistedGrant == default)
        {
            throw new Exception($"Persisted grant not found for client {clientId} and subject {subjectId}");
        }

        if (outbackPersistedGrant.Client is null)
        {
            throw new Exception($"No client found for persisted grant for client {clientId} and subject {subjectId}");
        }

        return new PersistedGrant
        {
            ClientId = outbackPersistedGrant.Client.ClientId,
            Created = outbackPersistedGrant.Created,
            Expires = outbackPersistedGrant.Expires,
            Scope = outbackPersistedGrant.Scope,
            SubjectId = outbackPersistedGrant.SubjectId.ToString()
        };
    }

    public async Task<RefreshTokenGrant> GetRefreshTokenGrantAsync(string refreshToken)
    {
        // Look up the grant regardless of its resolved state so that reuse of an
        // already-consumed token can be detected (RFC 9700 §10.5).
        var outbackRefreshTokenGrant = await _outbackDbContext.RefreshTokenGrants.Include(m => m.Client).SingleOrDefaultAsync(m => m.RefreshToken == refreshToken);

        if (outbackRefreshTokenGrant == default)
        {
            throw new Exception("Refresh token not found");
        }

        if (outbackRefreshTokenGrant.Client is null)
        {
            throw new Exception("No client found for persisted grant");
        }

        var wasResolved = outbackRefreshTokenGrant.Resolved;

        if (wasResolved is null)
        {
            outbackRefreshTokenGrant.Resolved = DateTimeOffset.Now;
            await _outbackDbContext.SaveChangesAsync();
        }

        return new RefreshTokenGrant
        {
            ClientId = outbackRefreshTokenGrant.Client.ClientId,
            Created = outbackRefreshTokenGrant.Created,
            Expires = outbackRefreshTokenGrant.Expires,
            Scope = outbackRefreshTokenGrant.Scope,
            SubjectId = outbackRefreshTokenGrant.SubjectId.ToString(),
            RefreshToken = outbackRefreshTokenGrant.RefreshToken,
            FamilyId = outbackRefreshTokenGrant.FamilyId,
            Resolved = wasResolved,
        };
    }

    public async Task RevokeRefreshTokenFamilyAsync(Guid familyId)
    {
        var familyGrants = await _outbackDbContext.RefreshTokenGrants.Where(m => m.FamilyId == familyId).ToListAsync();

        foreach (var grant in familyGrants)
        {
            grant.Resolved = DateTimeOffset.Now;
        }

        await _outbackDbContext.SaveChangesAsync();
    }

    public async Task SaveCodeGrantAsync(AuthorizationCodeGrant codeGrant)
    {
        var clientIntId = await _outbackDbContext.Clients.Where(m => m.ClientId == codeGrant.ClientId).Select(m => m.Id).SingleAsync();

        var outbackCodeGrant = new OutbackCodeGrant
        {
            ClientId = clientIntId,
            Code = codeGrant.Code,
            CodeChallenge = codeGrant.CodeChallenge,
            CodeChallengeMethod = codeGrant.CodeChallengeMethod,
            Created = codeGrant.Created,
            Expires = codeGrant.Expires,
            Nonce = codeGrant.Nonce,
            RedirectUri = codeGrant.RedirectUri,
            Scope = codeGrant.Scope,
            State = codeGrant.State,
            SubjectId = Guid.Parse(codeGrant.SubjectId),
        };

        await _outbackDbContext.CodeGrants.AddAsync(outbackCodeGrant);

        await _outbackDbContext.SaveChangesAsync();
    }

    public async Task SaveDeviceAuthorizationGrantAsync(DeviceCodeGrant deviceAuthorizationGrant)
    {
        var clientIntId = await _outbackDbContext.Clients.Where(m => m.ClientId == deviceAuthorizationGrant.ClientId).Select(m => m.Id).SingleAsync();

        await _outbackDbContext.DeviceAuthorizationGrants.AddAsync(new OutbackDeviceAuthorizationGrant
        {
            ClientId = clientIntId,
            Created = DateTimeOffset.UtcNow,
            AccessIsRejected = deviceAuthorizationGrant.AccessIsRejected,
            DeviceCode = deviceAuthorizationGrant.DeviceCode,
            UserCode = deviceAuthorizationGrant.UserCode,
            Scope = deviceAuthorizationGrant.Scope,
            SubjectId = string.IsNullOrEmpty(deviceAuthorizationGrant.SubjectId) ? Guid.Empty : Guid.Parse(deviceAuthorizationGrant.SubjectId),
            UserCodeExpiration = deviceAuthorizationGrant.UserCodeExpiration,
            Interval = deviceAuthorizationGrant.Interval,
        });

        await _outbackDbContext.SaveChangesAsync();
    }

    public async Task ResolveDeviceAuthorizationGrantAsync(string deviceCode)
    {
        var outbackDeviceAuthorizationGrant = await _outbackDbContext.DeviceAuthorizationGrants.SingleOrDefaultAsync(m => m.DeviceCode == deviceCode);

        if (outbackDeviceAuthorizationGrant == default)
        {
            throw new Exception("Device authorization grant not found");
        }

        outbackDeviceAuthorizationGrant.Resolved = DateTimeOffset.Now;

        await _outbackDbContext.SaveChangesAsync();
    }

    public async Task SavePersistedGrantAsync(PersistedGrant persistedGrant)
    {
        var clientIntId = await _outbackDbContext.Clients.Where(m => m.ClientId == persistedGrant.ClientId).Select(m => m.Id).SingleAsync();

        await _outbackDbContext.PersistedGrants.AddAsync(new OutbackPersistedGrant
        {
            ClientId = clientIntId,
            Expires = persistedGrant.Expires,
            Scope = persistedGrant.Scope,
            SubjectId = Guid.Parse(persistedGrant.SubjectId),
        });

        await _outbackDbContext.SaveChangesAsync();

    }

    public async Task SaveRefreshTokenGrantAsync(RefreshTokenGrant refreshTokenGrant)
    {
        var clientIntId = await _outbackDbContext.Clients.Where(m => m.ClientId == refreshTokenGrant.ClientId).Select(m => m.Id).SingleAsync();

        await _outbackDbContext.RefreshTokenGrants.AddAsync(new OutbackRefreshTokenGrant
        {
            ClientId = clientIntId,
            Created = refreshTokenGrant.Created,
            Expires = refreshTokenGrant.Expires,
            RefreshToken = refreshTokenGrant.RefreshToken,
            Scope = refreshTokenGrant.Scope,
            SubjectId = Guid.Parse(refreshTokenGrant.SubjectId),
            FamilyId = refreshTokenGrant.FamilyId,
        });

        await _outbackDbContext.SaveChangesAsync();
    }
}
