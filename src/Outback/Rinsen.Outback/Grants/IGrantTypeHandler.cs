using System.Threading.Tasks;
using Rinsen.Outback.Clients;
using Rinsen.Outback.Models;

namespace Rinsen.Outback.Grants
{
    public interface IGrantTypeHandler
    {
        public string GrantType { get; }

        Task<TokenResponse> GetTokenAsync(TokenModel tokenModel, Client client, string issuer);
        

    }

    public class TokenResponse 
    {
        /// <summary>
        /// Gets or sets the response containing the access token and related metadata.
        /// </summary>
        public AccessTokenResponse? AccessTokenResponse { get; set; }

        /// <summary>
        /// Gets or sets the error response associated with the operation.
        /// </summary>
        public ErrorResponse? ErrorResponse { get; set; }

        /// <summary>
        /// Gets a value indicating whether the response represents an error.
        /// </summary>
        public bool IsError => ErrorResponse != null;
    }
}
