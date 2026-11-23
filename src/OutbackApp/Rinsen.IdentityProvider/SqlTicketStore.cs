using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Rinsen.Outback.Claims;

namespace Rinsen.IdentityProvider;

public class SqlTicketStore : ITicketStore
{
    private readonly TicketSerializer _ticketSerializer = new();
    private readonly ISessionStorage _sessionStorage;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SqlTicketStore(ISessionStorage sessionStorage,
        IHttpContextAccessor httpContextAccessor)
    {
        _sessionStorage = sessionStorage;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task RemoveAsync(string key)
    {
        await _sessionStorage.DeleteAsync(key);
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var session = await _sessionStorage.GetAsync(key);

        if (ticket.Principal.Identity is not ClaimsIdentity claimsIdentity)
        {
            throw new Exception("No claims identity found");
        }

        if (session == default)
        {
            throw new Exception($"No session found for key {key}");
        }

        var expirationClaim = claimsIdentity.FindFirst(StandardClaims.Expiration);

        if (expirationClaim != default)
        {
            claimsIdentity.RemoveClaim(expirationClaim);
            claimsIdentity.AddClaim(new Claim(StandardClaims.Expiration, (ticket.Properties.ExpiresUtc ?? DateTimeOffset.Now.AddHours(1)).ToUnixTimeSeconds().ToString()));
        }

        var serializedTicket = _ticketSerializer.Serialize(ticket);

        session.SerializedTicket = serializedTicket;
        session.Updated = DateTimeOffset.Now;
        session.Expires = ticket.Properties.ExpiresUtc ?? DateTimeOffset.Now.AddHours(1);

        await _sessionStorage.UpdateAsync(session);
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var session = await _sessionStorage.GetAsync(key);

        if (session == default)
        {
            return null;
        }

        var ticket = _ticketSerializer.Deserialize(session.SerializedTicket);
        
        return ticket;
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var session = new Session
        {
            SessionId = ticket.Principal.GetClaimStringValue(StandardClaims.SessionId),
            IdentityId = ticket.Principal.GetClaimGuidValue(StandardClaims.Subject),
            CorrelationId = Guid.NewGuid(),
            UserAgent = string.Empty,
            IpAddress = string.Empty,
            Created = DateTimeOffset.Now,
            Updated = DateTimeOffset.Now,
            Deleted = null,
            Expires = ticket.Properties.ExpiresUtc ?? DateTimeOffset.Now.AddDays(1),
            SerializedTicket = _ticketSerializer.Serialize(ticket)
        };

        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext != null)
        {
            var remoteIpAddress = httpContext.Connection.RemoteIpAddress;
            if (remoteIpAddress != null)
            {
                session.IpAddress = remoteIpAddress.ToString();
            }

            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            if (!string.IsNullOrEmpty(userAgent))
            {
                session.UserAgent = userAgent.Length > 200 ? userAgent[..200] : userAgent;
            }
        }

        await _sessionStorage.CreateAsync(session);

        return session.SessionId;

    }
}
