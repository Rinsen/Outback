using Rinsen.Outback.Accessors;

namespace SampleServer.InMemoryAccessors
{
    public class UserInfoAccessor : IUserInfoAccessor
    {
        public Task<Dictionary<string, string>> GetUserInfoClaims(string subjectId, IEnumerable<string> scopes)
        {
            throw new NotImplementedException();
        }
    }
}
