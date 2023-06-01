namespace Rinsen.Outback.Configuration
{
    public class OutbackOptions
    {

        public bool UseDefaultConfigurationAccessor { get; set; } = true;

        public bool ClientCredentialsGrantActive { get; set; } = true;

        public bool DeviceAuthorizationGrantActive { get; set; } = true;

        public bool RefreshTokenGrantActive { get; set; } = true;

        public bool CodeGrantActive { get; set; } = true;

        public bool ClientSecretBasicActive { get; set; } = true;

        public bool ClientSecretPostActive { get; set; } = true;
    }
}
