using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Rinsen.IdentityProvider.AuditLogging;

public class AuditLog
{
    private readonly AuditLogStorage _auditLogStorage;

    public AuditLog(AuditLogStorage auditLogStorage)
    {
        _auditLogStorage = auditLogStorage;
    }

    // virtual so tests can substitute a no-op sink (SEC-020); mirrors the
    // testability pattern used for PasswordHashGenerator.GetPasswordHash.
    public virtual async Task Log(string eventType, string details, string ipAddress)
    {
        var auditLogItem = new AuditLogItem
        {
            EventType = eventType,
            Details = details,
            IpAddress = ipAddress,
            Timestamp = DateTimeOffset.Now
        };

       await _auditLogStorage.LogAsync(auditLogItem);
    }
}
