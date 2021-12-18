using System.Collections.Concurrent;
using Rinsen.Outback.Accessors;
using Rinsen.Outback.Grants;

namespace SampleServer.InMemoryAccessors
{
    public class GrantAccessor : IGrantAccessor
    {
        private readonly ConcurrentDictionary<string, CodeGrant> _persistedGrants = new ConcurrentDictionary<string, CodeGrant>();

        public Task<CodeGrant> GetCodeGrant(string code)
        {
            var codeGrant = _persistedGrants.GetValueOrDefault(code);

            if (codeGrant == default)
            {
                throw new Exception("CodeGrant not found");
            }

            return Task.FromResult(codeGrant);
        }

        public Task<PersistedGrant> GetPersistedGrant(string clientId, Guid subjectId)
        {
            throw new NotImplementedException();
        }

        public Task<RefreshTokenGrant> GetRefreshTokenGrant(string refreshToken)
        {
            throw new NotImplementedException();
        }

        public Task SaveCodeGrant(CodeGrant codeGrant)
        {
            _persistedGrants.TryAdd(codeGrant.Code, codeGrant);

            return Task.CompletedTask;
        }

        public Task SavePersistedGrant(PersistedGrant persistedGrant)
        {
            throw new NotImplementedException();
        }

        public Task SaveRefreshTokenGrant(RefreshTokenGrant refreshTokenGrant)
        {
            throw new NotImplementedException();
        }
    }
}
