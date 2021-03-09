namespace Rinsen.Outback.Claims
{
    /// <summary>
    /// Claim types from JWT and OpenId Connect spec
    /// https://openid.net/specs/openid-connect-core-1_0.html
    /// https://www.iana.org/assignments/jwt/jwt.xhtml
    /// </summary>
    public class StandardClaims
    {
        /// <summary>
        /// Issuer
        /// string
        /// RFC7519
        /// </summary>
        public const string Issuer = "iss";

        /// <summary>
        /// Audience
        /// string
        /// RFC7519
        /// </summary>
        public const string Audience = "aud";

        /// <summary>
        /// Expiration
        /// string
        /// RFC7519
        /// </summary>
        public const string Expiration = "exp";

        /// <summary>
        /// Not before
        /// string
        /// RFC7519
        /// </summary>
        public const string NotBefore = "nbf";
        /// <summary>
        /// Issued at
        /// string
        /// RFC7519
        /// </summary>
        public const string IssuedAt = "iat";

        /// <summary>
        /// JWT ID
        /// string
        /// RFC7519
        /// </summary>
        public const string JwtId = "jti";

        /// <summary>
        /// Identifier for the End-User at the Issuer.
        /// string
        /// RFC7519
        /// </summary>
        public const string Subject = "sub";

        /// <summary>
        /// Session ID
        /// OpenID Connect Front-Channel Logout 1.0
        /// </summary>
        public const string SessionId = "sid";

        /// <summary>
        /// End-User's full name in displayable form including all name parts, possibly including titles and suffixes, ordered according to the End-User's locale and preferences.
        /// string
        /// OpenID Connect Core 1.0
        /// </summary>
        public const string Name = "name";

        /// <summary>
        /// Given name(s) or first name(s) of the End-User.Note that in some cultures, people can have multiple given names; all can be present, with the names being separated by space characters.
        /// string
        /// OpenID Connect Core 1.0
        /// </summary>
        public const string GivenName = "given_name";

        /// <summary>
        /// Surname(s) or last name(s) of the End-User.Note that in some cultures, people can have multiple family names or no family name; all can be present, with the names being separated by space characters.
        /// string 
        /// OpenID Connect Core 1.0
        /// </summary>
        public const string FamilyName = "family_name";

        /// <summary>
        /// Middle name(s) of the End-User.Note that in some cultures, people can have multiple middle names; all can be present, with the names being separated by space characters.Also note that in some cultures, middle names are not used.
        /// string 
        /// OpenID Connect Core 1.0
        /// </summary>
        public const string MiddleName = "middle_name";

        /// <summary>
        /// Casual name of the End-User that may or may not be the same as the given_name. For instance, a nickname value of Mike might be returned alongside a given_name value of Michael.
        /// string 
        /// OpenID Connect Core 1.0
        /// </summary>
        public const string Nickname = "nickname";

        /// <summary>
        /// Shorthand name by which the End-User wishes to be referred to at the RP, such as janedoe or j.doe.This value MAY be any valid JSON string including special characters such as @, /, or whitespace. The RP MUST NOT rely upon this value being unique, as discussed in Section 5.7.
        /// string 
        /// OpenID Connect Core 1.0
        /// </summary>
        public const string PreferredUsername = "preferred_username";

        /// <summary>
        /// URL of the End-User's profile page. The contents of this Web page SHOULD be about the End-User.
        /// string 
        /// OpenID Connect Core 1.0
        /// </summary>
        public const string Profile = "profile";
        /// <summary>
        /// URL of the End-User's profile picture. This URL MUST refer to an image file (for example, a PNG, JPEG, or GIF image file), rather than to a Web page containing an image. Note that this URL SHOULD specifically reference a profile photo of the End-User suitable for displaying when describing the End-User, rather than an arbitrary photo taken by the End-User.
        /// string 
        /// OpenID Connect Core 1.0
        /// </summary>
        public const string Picture = "picture";

        /// <summary>
        /// URL of the End-User's Web page or blog. This Web page SHOULD contain information published by the End-User or an organization that the End-User is affiliated with.
        /// string 
        /// OpenID Connect Core 1.0
        /// </summary>
        public const string Website = "website";

        /// <summary>
        /// End-User's preferred e-mail address. Its value MUST conform to the RFC 5322 [RFC5322] addr-spec syntax. The RP MUST NOT rely upon this value being unique, as discussed in Section 5.7.
        /// string 
        /// OpenID Connect Core 1.0
        /// </summary>
        public const string Email = "email";

        /// <summary>
        /// True if the End-User's e-mail address has been verified; otherwise false. When this Claim Value is true, this means that the OP took affirmative steps to ensure that this e-mail address was controlled by the End-User at the time the verification was performed. The means by which an e-mail address is verified is context-specific, and dependent upon the trust framework or contractual agreements within which the parties are operating.
        /// boolean 
        /// OpenID Connect Core 1.0
        /// </summary>
        public const string EmailVerified = "email_verified";

        /// <summary>
        /// End-User's gender. Values defined by this specification are female and male. Other values MAY be used when neither of the defined values are applicable.
        /// string 
        /// OpenID Connect Core 1.0
        /// </summary>
        public const string Gender = "gender";

        /// <summary>
        /// End-User's birthday, represented as an ISO 8601:2004 [ISO8601‑2004] YYYY-MM-DD format. The year MAY be 0000, indicating that it is omitted. To represent only the year, YYYY format is allowed. Note that depending on the underlying platform's date related function, providing just year can result in varying month and day, so the implementers need to take this factor into account to correctly process the dates.
        /// string 
        /// OpenID Connect Core 1.0
        /// </summary>
        public const string Birthdate = "birthdate";

        /// <summary>
        /// String from zoneinfo [zoneinfo] time zone database representing the End-User's time zone. For example, Europe/Paris or America/Los_Angeles.
        /// string 
        /// OpenID Connect Core 1.0
        /// </summary>
        public const string Zoneinfo = "zoneinfo";

        /// <summary>
        /// End-User's locale, represented as a BCP47 [RFC5646] language tag. This is typically an ISO 639-1 Alpha-2 [ISO639‑1] language code in lowercase and an ISO 3166-1 Alpha-2 [ISO3166‑1] country code in uppercase, separated by a dash. For example, en-US or fr-CA. As a compatibility note, some implementations have used an underscore as the separator rather than a dash, for example, en_US; Relying Parties MAY choose to accept this locale syntax as well.
        /// string 
        /// OpenID Connect Core 1.0
        /// </summary>
        public const string Locale = "locale";

        /// <summary>
        /// End-User's preferred telephone number. E.164 [E.164] is RECOMMENDED as the format of this Claim, for example, +1 (425) 555-1212 or +56 (2) 687 2400. If the phone number contains an extension, it is RECOMMENDED that the extension be represented using the RFC 3966 [RFC3966] extension syntax, for example, +1 (604) 555-1234;ext=5678.
        /// string 
        /// OpenID Connect Core 1.0
        /// </summary>
        public const string PhoneNumber = "phone_number";

        /// <summary>
        /// True if the End-User's phone number has been verified; otherwise false. When this Claim Value is true, this means that the OP took affirmative steps to ensure that this phone number was controlled by the End-User at the time the verification was performed. The means by which a phone number is verified is context-specific, and dependent upon the trust framework or contractual agreements within which the parties are operating. When true, the phone_number Claim MUST be in E.164 format and any extensions MUST be represented in RFC 3966 format.
        /// boolean
        /// OpenID Connect Core 1.0
        /// </summary>
        public const string PhoneNumberVerified = "phone_number_verified";

        /// <summary>
        /// End-User's preferred postal address. The value of the address member is a JSON [RFC4627] structure containing some or all of the members defined in Section 5.1.1.
        /// JSON object
        /// OpenID Connect Core 1.0
        /// </summary>
        public const string Address = "address";

        /// <summary>
        /// Time the End-User's information was last updated. Its value is a JSON number representing the number of seconds from 1970-01-01T0:0:0Z as measured in UTC until the date/time.
        /// string 
        /// OpenID Connect Core 1.0
        /// </summary>
        public const string UpdatedAt = "updated_at";
    }
}
