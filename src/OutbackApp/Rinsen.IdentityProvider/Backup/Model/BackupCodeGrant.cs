using System;

namespace Rinsen.IdentityProvider.Backup.Model
{
    internal class BackupCodeGrant
    {
        public string Code { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public Guid SubjectId { get; set; }
    }
}
