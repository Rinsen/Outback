using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Models;

namespace Rinsen.Outback.Grants
{
    public class GrantService
    {
        private readonly ConcurrentDictionary<string, Grant> _persistedGrants = new ConcurrentDictionary<string, Grant>();
        private readonly RandomStringGenerator _randomStringGenerator;

        public GrantService(RandomStringGenerator randomStringGenerator)
        {
            _randomStringGenerator = randomStringGenerator;
        }

        public Task<Grant> GetGrant(string code, string clientId, string codeVerifier)
        {
            _persistedGrants.TryGetValue(code, out var persistedGrant);

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

            return Task.FromResult(persistedGrant);
        }

        public Task<string> CreateAndStoreGrant(Client client, ClaimsPrincipal user, AuthorizeModel model)
        {
            var code = _randomStringGenerator.GetRandomString(15);
            string refreshToken = null;

            if (!user.Claims.Any(m => m.Type == "sub"))
            {
                throw new SecurityException("sub claim not found");
            }

            var subjectId = user.FindFirstValue("sub");

            if (string.IsNullOrEmpty(subjectId))
            {
                throw new SecurityException("sub claim empty is not supported");
            }

            if (client.IssueRefreshToken)
            {
                refreshToken = _randomStringGenerator.GetRandomString(30);
            }

            if (!AbnfValidationHelper.IsValid(model.CodeChallenge, 43, 128))
            {
                // Code verifier is not valid
                throw new SecurityException("Code challange is not valid");
            }

            _persistedGrants.TryAdd(code, new Grant
            {
                ClientId = client.ClientId,
                Code = code,
                CodeChallange = model.CodeChallenge,
                CodeChallangeMethod = model.CodeChallengeMethod,
                Nonce = model.Nonce,
                RedirectUri = model.RedirectUri,
                ResponseType = model.ResponseType,
                RefreshToken = refreshToken,
                Scope = model.Scope,
                State = model.State,
                SubjectId = subjectId
            });

            return Task.FromResult(code);
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
