using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public PersistedGrant GetGrant(string code)
        {
            _persistedGrants.TryGetValue(code, out var persisted);

            return persisted;
        }

        public string CreateAndStoreGrant(string clientId, string subjectId, string codeChallenge, string codeChallengeMethod, string nonce, string redirectUri, string scope, string state, string responseType)
        {
            var code = _randomStringGenerator.GetRandomString(15);
            _persistedGrants.TryAdd(code, new PersistedGrant
            {
                ClientId = clientId,
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

            return code;
        }
    }
}
