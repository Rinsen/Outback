using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Rinsen.Outback.Abstractons;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Models;

namespace Rinsen.Outback.Grants
{
    public class GrantService
    {
        private readonly RandomStringGenerator _randomStringGenerator;
        private readonly IGrantStorage _grantStorage;

        public GrantService(RandomStringGenerator randomStringGenerator,
            IGrantStorage grantStorage)
        {
            _randomStringGenerator = randomStringGenerator;
            _grantStorage = grantStorage;
        }

        public async Task<Grant> GetGrant(string code, string clientId, string codeVerifier)
        {
            var persistedGrant = await _grantStorage.GetGrant(code);

            if (persistedGrant.Expires != null && persistedGrant.Expires > DateTime.UtcNow)
            {
                throw new SecurityException("Grant has expired");
            }

            if (persistedGrant.ClientId != clientId)
            {
                throw new SecurityException("Client id not matching");
            }

            if (!AbnfValidationHelper.IsValid(codeVerifier, 43, 128))
            {
                // Code verifier is not valid
                throw new SecurityException("Code verifier is not valid");
            }

            // Validate code
            using var sha256 = SHA256.Create();
            var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            var codeChallenge = WebEncoders.Base64UrlEncode(challengeBytes);

            if (codeChallenge != persistedGrant.CodeChallange)
            {
                throw new SecurityException("Code verifier is not matching code challenge");
            }

            return persistedGrant;
        }

        public async Task<string> CreateAndStoreGrant(Client client, ClaimsPrincipal user, AuthorizeModel model)
        {
            if (!AbnfValidationHelper.IsValid(model.CodeChallenge, 43, 128))
            {
                // Code verifier is not valid
                throw new SecurityException("Code challange is not valid");
            }

            var grant = new Grant
            {
                ClientId = client.ClientId,
                Code = _randomStringGenerator.GetRandomString(15),
                CodeChallange = model.CodeChallenge,
                CodeChallangeMethod = model.CodeChallengeMethod,
                Nonce = model.Nonce,
                RedirectUri = model.RedirectUri,
                ResponseType = model.ResponseType,
                Scope = model.Scope,
                State = model.State,
                Expires = DateTime.UtcNow.AddSeconds(client.GrantLifetime)
            };

            SetSubjectId(user, grant);

            if (client.IssueRefreshToken)
            {
                GenerateRefreshToken(grant, client);
            }

            await _grantStorage.Save(grant);

            return grant.Code;
        }

        private void GenerateRefreshToken(Grant grant, Client client)
        {
            grant.RefreshToken = _randomStringGenerator.GetRandomString(30);
            grant.RefreshTokenCreated = DateTime.UtcNow;
            grant.RefreshTokenExpires = DateTime.UtcNow.AddSeconds(client.RefreshTokenLifetime);
        }

        private static void SetSubjectId(ClaimsPrincipal user, Grant grant)
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

        public Task<string> GetCodeForExistingConsent(Client client, ClaimsPrincipal user, AuthorizeModel model)
        {
            throw new NotImplementedException();
        }

        public Task<Grant> GetGrant(string refreshToken, string clientId)
        {
            throw new NotImplementedException();
        }
    }
}
