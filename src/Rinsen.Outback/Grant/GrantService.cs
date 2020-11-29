using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Rinsen.Outback.Clients;

namespace Rinsen.Outback.Grant
{
    public class GrantService
    {
        private readonly ConcurrentDictionary<string, PersistedGrant> _persistedGrants = new ConcurrentDictionary<string, PersistedGrant>();
        private readonly RandomStringGenerator _randomStringGenerator;

        public GrantService(RandomStringGenerator randomStringGenerator)
        {
            _randomStringGenerator = randomStringGenerator;
        }

        public Task<PersistedGrant> GetGrant(string code, string clientId, string codeVerifier)
        {
            _persistedGrants.TryGetValue(code, out var persistedGrant);

            if (persistedGrant.ClientId != clientId)
            {
                throw new Exception();
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

        public Task<string> CreateAndStoreGrant(Client client, string subjectId, string codeChallenge, string codeChallengeMethod, string nonce, string redirectUri, string scope, string state, string responseType)
        {
            var code = _randomStringGenerator.GetRandomString(15);

            _persistedGrants.TryAdd(code, new PersistedGrant
            {
                ClientId = client.ClientId,
                Code = code,
                CodeChallange = codeChallenge,
                CodeChallangeMethod = codeChallengeMethod,
                Nonce = nonce,
                RedirectUri = redirectUri,
                ResponseType = responseType,
                Scope = scope,
                State = state,
                SubjectId = subjectId
            });

            return Task.FromResult(code);
        }
    }
}
