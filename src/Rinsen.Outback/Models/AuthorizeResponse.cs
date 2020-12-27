namespace Rinsen.Outback.Models
{
    public class AuthorizeResponse
    {
        public string Code { get; set; }

        public string Scope { get; set; }

        public string State { get; set; }

        public string SessionState { get; set; }

        public string FormPostUri { get; set; }

    }
}
