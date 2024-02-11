using System;
using System.Linq;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Rinsen.Outback.Accessors;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Helpers;
using Rinsen.Outback.Models;

namespace Rinsen.Outback.Grants;

internal class GrantService : IGrantService
{
    private readonly RandomStringGenerator _randomStringGenerator;
    private readonly IGrantAccessor _grantAccessor;

    public GrantService(RandomStringGenerator randomStringGenerator,
        IGrantAccessor grantAccessor)
    {
        _randomStringGenerator = randomStringGenerator;
        _grantAccessor = grantAccessor;
    }

    public async Task<CodeGrant> GetCodeGrantAsync(string code, string clientId, string codeVerifier)
    {
        var codeGrant = await _grantAccessor.GetCodeGrantAsync(code);

        if (codeGrant.Expires < DateTimeOffset.UtcNow)
        {
            throw new SecurityException("Grant has expired");
        }

        if (codeGrant.ClientId != clientId)
        {
            throw new SecurityException("Client id not matching");
        }

        // Validate code
        var challengeBytes = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
        var codeChallenge = WebEncoders.Base64UrlEncode(challengeBytes);

        if (codeChallenge != codeGrant.CodeChallenge)
        {
            throw new SecurityException("Code verifier is not matching code challenge");
        }

        return codeGrant;
    }

    public async Task<string> CreateCodeAndStoreCodeGrantAsync(Client client, ClaimsPrincipal user, AuthorizeModel model)
    {
        if (!AbnfValidationHelper.IsValid(model.CodeChallenge, 43, 128))
        {
            // Code verifier is not valid
            throw new SecurityException("Code challenge is not valid");
        }

        var grant = new CodeGrant
        {
            ClientId = client.ClientId,
            Code = _randomStringGenerator.GetRandomString(15),
            CodeChallenge = model.CodeChallenge,
            CodeChallengeMethod = model.CodeChallengeMethod,
            Nonce = model.Nonce,
            RedirectUri = model.RedirectUri,
            Scope = model.Scope,
            State = model.State,
            Expires = DateTimeOffset.UtcNow.AddSeconds(client.AuthorityCodeLifetime),
            Created = DateTimeOffset.UtcNow
        };

        SetSubjectId(user, grant);

        await _grantAccessor.SaveCodeGrantAsync(grant);

        return grant.Code;
    }

    private static void SetSubjectId(ClaimsPrincipal user, CodeGrant grant)
    {
        if (!user.Claims.Any(m => m.Type == "sub"))
        {
            throw new SecurityException("sub claim not found");
        }

        var subjectId = user.FindFirstValue("sub");

        if (string.IsNullOrEmpty(subjectId))
        {
            throw new SecurityException("sub claim empty is not supported");
        }

        grant.SubjectId = subjectId;
    }

    public Task<string> GetCodeForExistingConsentAsync(Client client, ClaimsPrincipal user, AuthorizeModel model)
    {
        throw new NotImplementedException();
    }

    public async Task<RefreshTokenGrant> GetRefreshTokenGrantAsync(string refreshToken, string clientId)
    {
        var refreshTokenGrant = await _grantAccessor.GetRefreshTokenGrantAsync(refreshToken);

        if (refreshTokenGrant == default)
        {
            throw new SecurityException($"No RefreshToken found");
        }

        if (!string.Equals(refreshTokenGrant.ClientId, clientId))
        {
            throw new SecurityException($"RefreshToken is not valid for client");
        }

        return refreshTokenGrant;
    }

    public async Task<string> CreateRefreshTokenAsync(Client client, CodeGrant persistedGrant)
    {
        var refreshToken = _randomStringGenerator.GetRandomString(40);
        var refreshTokenGrant = new RefreshTokenGrant
        {
            ClientId = client.ClientId,
            Created = DateTimeOffset.UtcNow,
            Expires = DateTimeOffset.UtcNow.AddSeconds(client.RefreshTokenLifetime),
            RefreshToken = refreshToken,
            Scope = persistedGrant.Scope,
            SubjectId = persistedGrant.SubjectId,
        };

        await _grantAccessor.SaveRefreshTokenGrantAsync(refreshTokenGrant);

        return refreshToken;
    }

    public async Task<string> CreateNewRefreshTokenAsync(Client client, RefreshTokenGrant refreshTokenGrant)
    {
        var refreshToken = _randomStringGenerator.GetRandomString(40);
        var newRefreshTokenGrant = new RefreshTokenGrant
        {
            ClientId = client.ClientId,
            Created = DateTimeOffset.UtcNow,
            Expires = DateTimeOffset.UtcNow.AddSeconds(client.RefreshTokenLifetime),
            RefreshToken = refreshToken,
            Scope = refreshTokenGrant.Scope,
            SubjectId = refreshTokenGrant.SubjectId,
        };

        await _grantAccessor.SaveRefreshTokenGrantAsync(newRefreshTokenGrant);

        return refreshToken;
    }

    public async Task<DeviceAuthorizationGrant> GetDeviceAuthorizationGrantAsync(Client client, string scope)
    {
        var deviceCode = _randomStringGenerator.GetRandomString(40);
        var userCode = GetUserCode(12);

        var deviceAuthorizationGrant = new DeviceAuthorizationGrant
        {
            ClientId = client.ClientId,
            DeviceCode = deviceCode,
            Interval = 5,
            SubjectId = null,
            Scope = scope,
            UserCode = userCode,
            UserCodeExpiration = DateTimeOffset.UtcNow.AddSeconds(client.DeviceCodeUserCompletionLifetime)
        };

        await _grantAccessor.SaveDeviceAuthorizationGrantAsync(deviceAuthorizationGrant);

        return deviceAuthorizationGrant;
    }

    private static string GetUserCode(int length)
    {
        const string sourceString = "BCDFGHJKLMNPQRSTVWXZ";

        byte[] randomBytes = new byte[length];

        using var randomNumberGenerator = RandomNumberGenerator.Create();

        randomNumberGenerator.GetBytes(randomBytes);

        var result = new StringBuilder(length + 2);

        var count = 0;
        foreach (byte b in randomBytes)
        {
            if (count == 4 || count == 9)
            {
                result.Append('-');
                count++;
            }

            result.Append(sourceString[b % sourceString.Length]);
            count++;
        }

        return result.ToString();
    }
}
