using Rinsen.IdentityProvider.Backup.Model;

namespace Rinsen.IdentityProvider.Backup.Model
{
    public class BackupRoot
    {
        public string Name { get; set; } = "OutbackAppBackup";

        public string Description { get; set; } = "This is a backup for restoring Outback App database state via API";

        public string Version { get; set; } = "1.0";

        public BackupContent Content { get; set; } = new BackupContent();

    }
}
