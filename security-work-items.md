# Outback — Security Remediation Work Items

**Source:** `security-audit-report.html` (2026-11-24)
**Scope:** `E:\repos\Rinsen\Outback`
**Status legend:** `Open` → `In Progress` → `Done`
**Priority:** P1 = High · P2 = Medium · P3 = Low

> Every item is tied to a verified file + line from the audit. Acceptance criteria are the
> definition of done. Work items are ordered by severity within each area.

## Progress summary

| Area | P1 | P2 | P3 | Total |
|---|---|---|---|---|
| 1 · OAuth 2.1 / OIDC | 3 | 4 | 2 | 9 |
| 2 · Token handling (.NET) | 0 | 5 | 1 | 6 |
| 3 · Auth / login flow | 4 | 1 | 3 | 8 |
| **Security total** | **7** | **10** | **6** | **23** |
| Functional (context) | 0 | 0 | 5 | 5 |

---

## Area 1 — OAuth 2.1 / OIDC Protocol Compliance

### SEC-001 · [P1] Validate refresh-token expiration
- **Status:** Done (2026-11-24)
- **Location:** `src/Outback/Rinsen.Outback/Grants/RefreshTokenGrantTypeHandler.cs:61–76` · `src/OutbackApp/Rinsen.IdentityProvider/Outback/GrantAccessor.cs:87–113`
- **Problem:** `Expires` is set on the grant (`RefreshTokenGrantTypeHandler.cs:85`) but never checked, so expired refresh tokens remain valid indefinitely.
- **Acceptance criteria:**
  - [x] `GetRefreshTokenGrantAsync` rejects grants where `Expires < DateTimeOffset.UtcNow`
  - [x] Rejection returns `invalid_grant` (no stack/secret leakage)
  - [x] Test: expired refresh token → `invalid_grant`; unexpired → success
- **Resolution:** Expiration check added in `RefreshTokenGrantTypeHandler.GetRefreshTokenGrantAsync` (mirrors the `AuthorizationCodeGrantTypeHandler` pattern): expired grant → `SecurityException("Grant has expired")` → caught in `GetTokenAsync` → clean `invalid_grant` response with no `error_description`. Covered by `tests/Rinsen.Outback.Tests/RefreshTokenExpiration.cs` (`ExpiredRefreshToken_ReturnsInvalidGrant`, `UnexpiredRefreshToken_ReturnsAccessToken`), both passing. Note: SEC-002 (token logged on the failure path, line 49) remains open and was intentionally not touched.
- **References:** RFC 6749 §6, RFC 9700 §4.12

### SEC-002 · [P1] Stop logging refresh tokens in plaintext
- **Status:** Done (2026-11-24)
- **Location:** `src/Outback/Rinsen.Outback/Grants/RefreshTokenGrantTypeHandler.cs:49` · `GrantAccessor.cs:93,98`
- **Problem:** Raw refresh token is written to the log on the (attacker-triggerable) failure path, and embedded in exception messages that are then logged.
- **Acceptance criteria:**
  - [x] Remove `{RefreshToken}` from the `LogError` call at line 49
  - [x] Remove the token from exception messages in `GrantAccessor.cs:93,98`
  - [x] Repo-wide grep confirms no refresh token appears in any log statement
  - [x] Test: failed refresh attempt produces a log line without the token value
- **Resolution:** `{RefreshToken}` removed from the `LogError` call in `RefreshTokenGrantTypeHandler.GetTokenAsync` (client id still logged); `GrantAccessor.GetRefreshTokenGrantAsync` exceptions now read "Refresh token not found" / "No client found for persisted grant" with no token interpolation. Repo-wide grep confirms no remaining log statement or exception message embeds a refresh token. Covered by `tests/Rinsen.Outback.Tests/RefreshTokenLogging.cs` (`FailedRefreshAttempt_LogContainsNoToken` — capturing logger asserts the token appears in neither the log message nor the logged exception) and `tests/Rinsen.IdentityProvider.Tests/GrantAccessorTests.cs` (both `GrantAccessor` failure paths assert the exception message contains no token; EF InMemory), all passing.
- **References:** RFC 9700 §10.5

### SEC-003 · [P1] Stop logging the PKCE code verifier
- **Status:** Done (2026-11-24)
- **Location:** `src/Outback/Rinsen.Outback/Grants/AuthorizationCodeGrantTypeHandler.cs:66`
- **Problem:** The code verifier (PKCE secret) is logged when a malformed verifier is submitted.
- **Acceptance criteria:**
  - [x] Remove `{CodeVerifier}` from the `LogError` call at line 66
  - [x] Repo-wide grep confirms no `code_verifier`/`CodeVerifier` value is logged
  - [x] Test: invalid verifier → `invalid_request`, log contains no verifier
- **Resolution:** `{CodeVerifier}` removed from the `LogError` call in `AuthorizationCodeGrantTypeHandler.GetTokenAsync` (client id still logged). Repo-wide grep confirms no log statement, string interpolation, exception message, or console output embeds a code verifier. Covered by `tests/Rinsen.Outback.Tests/CodeVerifierLogging.cs` (`InvalidCodeVerifier_ReturnsInvalidRequest_AndLogContainsNoVerifier` — asserts `invalid_request` and that the verifier appears in no captured log entry). Shared `CapturingLogger<T>` helper added in `tests/Rinsen.Outback.Tests/Helpers/CapturingLogger.cs` (also used by the SEC-002 test).
- **References:** RFC 7636 §4.6

### SEC-004 · [P2] Add refresh-token reuse detection & family revocation
- **Status:** Done (2026-11-24)
- **Location:** `src/Outback/Rinsen.Outback/Grants/RefreshTokenGrantTypeHandler.cs:40–59` · `GrantAccessor.cs:87–113`
- **Problem:** Replaying a consumed token only fails with `invalid_grant`; the live family token stays valid. No theft detection.
- **Acceptance criteria:**
  - [x] Add `FamilyId` to `OutbackRefreshTokenGrant` (DB migration)
  - [x] Rotation inherits the source grant's `FamilyId`
  - [x] Reuse of a `Resolved` token revokes all grants in the family
  - [x] Tests: normal rotation works; replay of an old token revokes the family
- **Resolution:** `FamilyId` (`Guid`) added to the `RefreshTokenGrant` model and the `OutbackRefreshTokenGrant` entity; the `FamilyId` column was added directly to the v3 table creation in `OutbackTableInstallation.cs` (no new database version needed — the system is not installed in any real environment yet). `AuthorizationCodeGrantTypeHandler.CreateRefreshTokenAsync` starts a new family (`Guid.NewGuid()`); `RefreshTokenGrantTypeHandler.CreateNewRefreshTokenAsync` inherits the source grant's `FamilyId` (generates a fresh one when the source is a legacy grant with `Guid.Empty`). `GrantAccessor.GetRefreshTokenGrantAsync` now looks the grant up by token regardless of its `Resolved` state, resolves it if it was not yet resolved, and reports the prior `Resolved` state via the new `RefreshTokenGrant.Resolved` property. New `IGrantAccessor.RevokeRefreshTokenFamilyAsync(Guid)` marks every grant of the family as resolved (implemented in `Rinsen.IdentityProvider.Outback.GrantAccessor`, `SampleServer` in-memory accessor, and the test fake). `RefreshTokenGrantTypeHandler.GetRefreshTokenGrantAsync` detects reuse (`Resolved != null`), logs a warning (client id + family id, no token value), revokes the whole family, and the request fails with a clean `invalid_grant`. Also fixed while in `SaveRefreshTokenGrantAsync`: `Created` is now persisted (previously left unset). Covered by `tests/Rinsen.Outback.Tests/RefreshTokenReuse.cs` (`NormalRotation_ReturnsAccessToken_AndInheritsFamilyId`, `NormalRotationOfLegacyGrant_GeneratesNewFamilyId`, `ReplayOfResolvedToken_ReturnsInvalidGrant_AndRevokesFamily`) and `tests/Rinsen.IdentityProvider.Tests/RefreshTokenFamilyTests.cs` (lookup/resolve semantics, family revocation leaves other families untouched, `FamilyId` persistence; EF InMemory), all passing.
- **References:** RFC 9700 §10.5

### SEC-005 · [P2] Make the device-code grant single-use
- **Status:** Done (2026-11-24)
- **Location:** `src/Outback/Rinsen.Outback/Grants/DeviceCodeGrantTypeHandler.cs:34–69`
- **Problem:** A device code can be redeemed repeatedly until `UserCodeExpiration`.
- **Acceptance criteria:**
  - [x] Mark the device grant resolved after a successful token issuance
  - [x] Second redemption of the same `device_code` → `invalid_grant`
  - [x] Implement/verify in the app's `GrantAccessor` (currently `NotImplementedException`)
  - [x] Test: first redemption succeeds, second fails
- **Resolution:** `Resolved` (`DateTimeOffset?`) added to the `DeviceCodeGrant` model and the `OutbackDeviceAuthorizationGrant` entity (kept separate from the existing `Accepted` user-consent timestamp); the `Resolved` column was added directly to the v3 table creation in `OutbackTableInstallation.cs` (no new database version needed — the system is not installed in any real environment yet). New `IGrantAccessor.ResolveDeviceAuthorizationGrantAsync(string)` marks the grant resolved. `DeviceCodeGrantTypeHandler.GetTokenAsync` now rejects a grant whose `Resolved` is set with a warning log (client id only, no device code) and `invalid_grant`; after a successful token issuance it calls `ResolveDeviceAuthorizationGrantAsync` so the code is consumed exactly once (polling lookups do not consume the grant, since resolution only happens after issuance). The app's `GrantAccessor` previously threw `NotImplementedException` for the device methods: `GetDeviceAuthorizationGrantAsync` now looks the grant up by device code regardless of its resolved state (so reuse can be detected), `SaveDeviceAuthorizationGrantAsync` persists the grant (empty `SubjectId` maps to `Guid.Empty`, `Created` set on save), and `ResolveDeviceAuthorizationGrantAsync` stamps `Resolved` — all implemented in `Rinsen.IdentityProvider.Outback.GrantAccessor`, the `SampleServer` in-memory accessor, and the test fake (which records `ResolvedDeviceCodes`). Covered by `tests/Rinsen.Outback.Tests/DeviceCodeReuse.cs` (`FirstRedemption_ReturnsAccessToken_AndResolvesGrant`, `SecondRedemptionOfResolvedGrant_ReturnsInvalidGrant`) and `tests/Rinsen.IdentityProvider.Tests/DeviceCodeGrantTests.cs` (lookup/resolve semantics and persistence; EF InMemory), all passing. Note: the pre-existing `DeviceAuthorizationFlow.TestingBasicFlow` integration test still fails for its original reason (SampleServer does not have the device grant type active for `DeviceAuthorizationClientId`) — unchanged by this work.
- **References:** RFC 8628 §3.2

### SEC-006 · [P2] Fix swapped access-token / ID-token lifetimes
- **Status:** Done (2026-11-24)
- **Location:** `src/Outback/Rinsen.Outback/JwtTokens/TokenService.cs:107,142,203` (expires_in at `:93`)
- **Problem:** Access-token `exp` uses `IdentityTokenLifetime`; ID-token `exp` uses `AccessTokenLifetime`; `exp` disagrees with advertised `expires_in`.
- **Acceptance criteria:**
  - [x] Access token (lines 107, 203) `exp` = `client.AccessTokenLifetime`
  - [x] ID token (line 142) `exp` = `client.IdentityTokenLifetime`
  - [x] `expires_in` matches the issued access-token `exp`
  - [x] Tests assert `exp` and `expires_in` are consistent per token type
- **Resolution:** The lifetimes were simply swapped in `TokenService.cs`: `CreateAccessToken` (line 107) now sets `Expires = DateTime.UtcNow.AddSeconds(client.AccessTokenLifetime)`, `CreateIdentityToken` (line 142) now sets `Expires = DateTime.UtcNow.AddSeconds(client.IdentityTokenLifetime)`, and the client-credentials path (line 203) now uses `client.AccessTokenLifetime`. No change was needed to the `expires_in` values — every `ExpiresIn` assignment already used `client.AccessTokenLifetime`, so with the corrected `exp` the advertised `expires_in` now matches the issued access-token `exp` (both derive from `AccessTokenLifetime`; `exp - iat` equals the lifetime up to whole-second truncation). Covered by `tests/Rinsen.Outback.Tests/TokenLifetimes.cs` (new): a client with distinct lifetimes (access 100 s, identity 300 s) issues tokens through the real `TokenService` with fake signing/user-info/scope accessors, and each test decodes the JWT and asserts `exp - iat` falls in `[lifetime, lifetime + 1]` for the correct token type plus `expires_in == AccessTokenLifetime` — `AuthorizationCodeFlow_AccessToken_ExpiresAfterAccessTokenLifetime`, `AuthorizationCodeFlow_IdentityToken_ExpiresAfterIdentityTokenLifetime`, `ClientCredentials_AccessToken_ExpiresAfterAccessTokenLifetime`, all passing. Verified the tests are not vacuous: with the swap re-introduced temporarily, the two authorization-code-flow tests fail (the client-credentials test still passed, as its line was already fixed), and all three pass again with the fix in place. Regression: `Rinsen.Outback.Tests` 18 total with only the 4 known pre-existing failures; `Rinsen.IdentityProvider.Tests` 11/11.

### SEC-007 · [P2] Enforce TOTP single-use (check the used-code log)
- **Status:** Done (2026-11-24)
- **Location:** `src/OutbackApp/Rinsen.IdentityProvider/LoginService.cs:134–141` · `IUsedTotpLogStorage` · `UsedTotpLogStorage.cs`
- **Problem:** `UsedTotpLog` is written but never read; a code is replayable within the ±1-step window.
- **Acceptance criteria:**
  - [x] Add `HasBeenUsedAsync(identityId, code)` to `IUsedTotpLogStorage` (+ SQL)
  - [x] `ConfirmTotpCode` checks it before verifying and throws `TotpCodeAlreadyUsedException`
  - [x] Prune stale `UsedTotpLog` rows (e.g., > 10 min) to bound table growth
  - [x] Tests: first code succeeds, immediate replay is rejected
- **Resolution:** `IUsedTotpLogStorage` gained `HasBeenUsedAsync(Guid identityId, string code)` and `PruneStaleAsync(TimeSpan maxAge)`; `UsedTotpLogStorage` implements both with ADO.NET SQL — `SELECT COUNT(1) FROM UsedTotpLogs WHERE IdentityId = @IdentityId AND Code = @Code` and `DELETE FROM UsedTotpLogs WHERE UsedTime < @Cutoff` (cutoff = `DateTimeOffset.Now - maxAge`). `LoginService.ConfirmTotpCode` now, after the TOTP-enabled check and before `VerifyTotp`, calls `PruneStaleAsync(UsedTotpLogMaxAge)` (10 min — safe, since a code is only verifiable for ~30 s) and then checks `HasBeenUsedAsync`; on a hit it logs a warning (identity id only, never the code) and throws `TotpCodeAlreadyUsedException`. The existing composite unique index `UX_UsedTotpLogs_IdentityId` on (IdentityId, Code) remains as the race-condition backstop: a concurrent insert still fails with 2601/2627 and `CreateLog` still maps that to `TotpCodeAlreadyUsedException`. Consistent with SEC-002/003, the code was removed from the `CreateLog` exception message (was `Totp Code '{code}' is already used for identity ...`, now identity id only). Supporting changes: `TotpCodeAlreadyUsedException` gained a message-only constructor (it only had `(message, innerException)`), and `TotpKeyNotValidException` was made public (it was `internal` but is thrown from the public `ILoginService.ConfirmTotpCode` and is asserted by tests). Covered by `tests/Rinsen.IdentityProvider.Tests/TotpSingleUse.cs` (new): a fake `IUsedTotpLogStorage` simulates the DB semantics (in-memory log list; `HasBeenUsedAsync` queries it; `PruneStaleAsync` removes stale rows and records the max age), with a real OtpNet `Totp` secret and codes — `ConfirmTotpCode_ValidCode_SucceedsAndLogsTheCode`, `ConfirmTotpCode_ImmediateReplayOfUsedCode_IsRejected` (also asserts the exception message does not contain the code), `ConfirmTotpCode_InvalidCode_ThrowsTotpKeyNotValidException` (deterministically picks a 6-digit code outside the ±1-step window and asserts nothing is logged), `ConfirmTotpCode_PrunesStaleUsedTotpLogRows` (asserts the 10-min prune is called), all passing. Verified the tests are not vacuous: with the `HasBeenUsedAsync` check temporarily disabled, the replay test fails ("No exception was thrown") and passes again with the check in place. Regression: `Rinsen.Outback.Tests` 18 total with only the 4 known pre-existing failures; `Rinsen.IdentityProvider.Tests` 15/15.
- **References:** RFC 6238

### SEC-008 · [P3] Complete the OIDC discovery document
- **Status:** Done (2026-11-24)
- **Location:** `src/Outback/Rinsen.Outback/Controllers/WellKnownController.cs:47–93` · `WellKnown/OpenIdConfiguration.cs`
- **Problem:** Missing REQUIRED `response_types_supported`, `id_token_signing_alg_values_supported`, and `userinfo_endpoint` (endpoint exists).
- **Acceptance criteria:**
  - [x] `response_types_supported` = `["code"]`
  - [x] `id_token_signing_alg_values_supported` = `["ES256"]`
  - [x] `userinfo_endpoint` present and reachable
  - [x] Discovery JSON validates against OIDC Core §15
- **Resolution:** `OpenIdConfiguration` gained the two missing properties — `UserInfoEndpoint` (`userinfo_endpoint`) and `IdTokenSigningAlgValuesSupported` (`id_token_signing_alg_values_supported`); `ResponseTypesSupported` and `SubjectTypesSupported` already existed but were never populated (serialized as `[]`). `WellKnownController.OpenIdConfiguration` now sets: `ResponseTypesSupported = ["code"]` (the authorize action only accepts `response_type=code` — `OAuthController.Authorize` rejects anything else); `IdTokenSigningAlgValuesSupported = ["ES256"]` (tokens are signed with `SecurityAlgorithms.EcdsaSha256` on P-256, see `TokenSigningAccessor`); `UserInfoEndpoint = https://{host}/oauth/userinfo` (the existing `[Authorize]` `UserInfo` action in `OAuthController`); and `SubjectTypesSupported = ["public"]` — required by OIDC Core §15 and factually correct, since `sub` is the raw identity identifier (a Guid), i.e. a public identifier, not a per-client salted pairwise one. Also fixed a pre-existing copy-paste bug while in the method: the `IsCodeGrantActiveAsync` block appeared twice, so `authorization_code` was added to `grant_types_supported` twice — the duplicate block was removed. Covered by `tests/Rinsen.Outback.Tests/WellKnownEndpoint.cs` (extended): `ResponseTypesSupported_ContainsCode`, `IdTokenSigningAlgValuesSupported_ContainsEs256`, `UserInfoEndpoint_IsPresentAndReachable` (asserts the endpoint is advertised and that an unauthenticated `GET oauth/userinfo` answers 401 rather than 404), `SubjectTypesSupported_ContainsPublic`, `GrantTypesSupported_ContainsNoDuplicates` (asserts `authorization_code` present exactly once) — all passing, plus the pre-existing `MustReturnInformationThatMakesSense`. Verified the tests are not vacuous: with the `ResponseTypesSupported` assignment temporarily removed, `ResponseTypesSupported_ContainsCode` fails ("Collections differ") and passes again with it in place. Regression: `Rinsen.Outback.Tests` 23 total with only the 4 known pre-existing failures; `Rinsen.IdentityProvider.Tests` 15/15. Note: the hard-coded `https` scheme in the discovery URLs (including the new `userinfo_endpoint`) is addressed by SEC-009.
- **References:** OIDC Core §15

### SEC-009 · [P3] Derive issuer / verification URI from the request scheme
- **Status:** Done (2026-11-24)
- **Location:** `src/Outback/Rinsen.Outback/Controllers/OAuthController.cs:262–272,354–357` · `WellKnownController.cs:44–53,78`
- **Problem:** Scheme hard-coded to `https`; issuer mismatches non-TLS deployments and `verification_uri` is unreachable.
- **Acceptance criteria:**
  - [x] `GetIssuer()`, `VerificationUri`, and discovery URLs use `HttpContext.Request.Scheme`
  - [x] Test over `http` and `https` yields a matching issuer
- **Resolution:** `OAuthController.GetIssuer()` now returns `HttpContext.Request.Scheme + "://" + host` (was hard-coded `https://`); it feeds the token `iss` claim (line 199) and the device-flow `VerificationUri`/`VerificationUriComplete`, which are now derived from it (`{GetIssuer()}/device`, was hard-coded `https://{host}/device`). `WellKnownController.OpenIdConfiguration` replaced `var host` with `var baseUrl = HttpContext.Request.Scheme + "://" + host` and uses it for all six advertised URLs: `issuer`, `jwks_uri`, `authorization_endpoint`, `token_endpoint`, `userinfo_endpoint`, and `device_authorization_endpoint`. Checked `Rinsen.Outback.App/ApiControllers/SessionController.cs` (login/logout URLs) — it already branches on `Request.IsHttps`, no change needed. Tests in `tests/Rinsen.Outback.Tests/WellKnownEndpoint.cs` (extended): `Issuer_MatchesRequestScheme_OverHttp` and `Issuer_MatchesRequestScheme_OverHttps` (clients created with `WebApplicationFactoryClientOptions.BaseAddress` of `http://localhost` / `https://localhost`; TestServer propagates the request URI's scheme/host into `HttpContext.Request`, and SampleServer's `UseHttpsRedirection` passes http traffic through because no https port is resolvable in TestServer), plus `DiscoveryUrls_UseTheSchemeOfTheRequest` (both schemes; all six URLs must start with the request's `scheme://localhost`); the pre-existing `MustReturnInformationThatMakesSense` was updated to expect `http://localhost` since the default test client speaks http. `CreateClientAsync` gained an optional `Uri baseAddress` parameter, and the test model `Helpers/OpenIdConfigurationModel.cs` gained `DeviceAuthorizationEndpoint` (`device_authorization_endpoint`). Verified the tests are not vacuous: with both controllers temporarily reverted to hard-coded `https`, exactly the three http-based tests fail (`Issuer_MatchesRequestScheme_OverHttp`, `DiscoveryUrls_UseTheSchemeOfTheRequest`, `MustReturnInformationThatMakesSense`) while the https tests still pass — i.e. the tests genuinely pin the scheme; restored, all 9 pass. Note: the token `iss` claim shares the same `GetIssuer()` code path, but an end-to-end token-issuer test is blocked by the pre-existing FUNC-001/002/003 failures (token endpoint returns 400 in the sample environment). Regression: `Rinsen.Outback.Tests` 26 total with only the 4 known pre-existing failures; `Rinsen.IdentityProvider.Tests` 15/15; full solution builds with 0 errors.
- **References:** RFC 8414 §5 (issuer), RFC 8628 §3.1 (verification_uri), OIDC Core §15 (issuer)

---

## Area 2 — Token Handling & Validation (.NET)

### SEC-010 · [P2] Explicit TokenValidationParameters + algorithm whitelist
- **Status:** Done (2026-11-24)
- **Location:** `src/OutbackApp/Rinsen.Outback.App/Startup.cs:163–167` (mirrored in `Program.cs`)
- **Problem:** No explicit `ValidAlgorithms` or `Validate*` flags; defaults only.
- **Acceptance criteria:**
  - [x] `TokenValidationParameters` sets `ValidateIssuer/Audience/Lifetime/IssuerSigningKey = true`
  - [x] `ValidAlgorithms = [EcdsaSha256]`, `ClockSkew = 5 min`
  - [x] Applied in both `Startup.cs` and `Program.cs`
  - [x] Test: a token signed with a non-ES256 key is rejected
- **Resolution:** Extracted the explicit `TokenValidationParameters` into a shared, testable helper `InboundTokenValidation.CreateTokenValidationParameters()` in the `Rinsen.Outback` library (`src/Outback/Rinsen.Outback/JwtTokens/InboundTokenValidation.cs`, namespace `Rinsen.Outback.JwtTokens`). It sets `ValidateIssuer`/`ValidateAudience`/`ValidateLifetime`/`ValidateIssuerSigningKey = true`, `ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256]`, and `ClockSkew = 5 min` — matching the algorithm the Outback issuer signs with (`SecurityAlgorithms.EcdsaSha256` on P-256, see `TokenSigningAccessor`). Applied in both app entry points: `Startup.cs` and `Program.cs` now set `options.TokenValidationParameters = InboundTokenValidation.CreateTokenValidationParameters()` in their `AddJwtBearer` calls (both files gained `using Rinsen.Outback.JwtTokens;`). The helper lives in the library rather than `Rinsen.Outback.App` because referencing the web project from the test project caused a NuGet version conflict (the app's `Microsoft.AspNetCore.Authentication.JwtBearer` 11.0.0 bumped the resolved `Microsoft.IdentityModel.Tokens` version and broke even the pre-existing `JwtValidationHelper`); the library's stable dependency set avoids that. Tests in `tests/Rinsen.Outback.Tests/InboundTokenValidationTests.cs` (new, 3 tests, all passing): `SetsExplicitValidationFlags_AlgorithmWhitelist_AndClockSkew` (asserts all four `Validate*` flags, the exact ES256-only whitelist, and the 5-minute skew), `TokenSignedWithNonEs256Key_IsRejected` (an HS256-signed token → `TokenValidationResult.IsValid == false`), and `TokenSignedWithEs256Key_IsAccepted` (an ES256-signed token with matching issuer/audience → valid). Tokens are built with `JsonWebTokenHandler` (`CreateToken(SecurityTokenDescriptor)` / `ValidateTokenAsync`) following the known-working pattern in `Helpers/JwtValidationHelper.cs`. Verified the tests are not vacuous: temporarily adding `SecurityAlgorithms.HmacSha256` to `ValidAlgorithms` made both `TokenSignedWithNonEs256Key_IsRejected` (the token was now accepted) and `SetsExplicitValidationFlags_AlgorithmWhitelist_AndClockSkew` fail, and both pass again with the whitelist restored. Regression: `Rinsen.Outback.Tests` 29 total with only the 4 known pre-existing FUNC-001/002/003 failures; `Rinsen.IdentityProvider.Tests` 15/15; `dotnet build Outback.slnx` 0 errors.
- **References:** Microsoft identity guidance, .NET `TokenValidationParameters`

### SEC-011 · [P2] Fix SampleServer empty Authority/Audience
- **Status:** Done (2026-11-24)
- **Location:** `sample/SampleServer/Startup.cs:31–53` · `sample/SampleServer/appsettings.json`
- **Problem:** `Authority = ""` / `Audience = ""` disables issuer + audience validation.
- **Acceptance criteria:**
  - [x] Set real `Authority` and `Audience` from configuration
  - [x] Add `ValidAlgorithms` whitelist
  - [ ] Or clearly mark the sample as validation-disabled and gate it behind a flag (not taken — validation is enabled, see Resolution)
  - [x] Test: token from a foreign issuer is rejected
- **Resolution:** Enabled real issuer + audience + signature validation in the SampleServer's `AddJwtBearer` (previously `Authority = ""` / `Audience = ""`, which disabled issuer + audience validation). Added a `Jwt` section to `sample/SampleServer/appsettings.json` (`Authority: "http://localhost"`, `Audience: "OutbackAS"`). The `TokenValidationParameters` now set `ValidateIssuer`/`ValidateAudience`/`ValidateLifetime`/`ValidateIssuerSigningKey = true`, `ValidIssuer` from `Jwt:Authority`, `ValidAudience` from `Jwt:Audience`, `ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256]` (matching the P-256 key the sample signs with), and `ClockSkew = 5 min`. The `IssuerSigningKey` is supplied directly from the SampleServer's `ITokenSigningAccessor` singleton so validation does not depend on a metadata fetch.
  - **Mechanism note:** we set `ValidIssuer`/`ValidAudience` (driven by the same `Jwt:Authority`/`Jwt:Audience` config values) rather than `options.Authority`/`options.Audience`. Setting `options.Authority` makes `JwtBearerPostConfigureOptions` create a `ConfigurationManager` (a `BaseConfigurationManager`) that lazily fetches signing keys from a metadata endpoint, which overrides the directly-supplied per-instance key (`JwtBearerHandler.SetupTokenValidationParametersAsync` assigns the manager to the parameters). The sample generates a fresh P-256 key per instance and does not serve it via a reachable metadata endpoint in all environments (including the in-process test host), so the metadata path fails with "no keys found" (IDX10500) → 401 even for a correctly-signed token. Supplying the key directly and validating `ValidIssuer`/`ValidAudience` achieves the same security outcome (real issuer + audience + signature validation from configuration) without the fragile metadata fetch.
  - **Tests:** `tests/Rinsen.Outback.Tests/SampleServerTokenValidation.cs` (new, 3 tests, all passing): `TokenSignedByForeignIssuer_IsRejected` (a token signed with a foreign P-256 key → 401), `TokenWithWrongAudience_IsRejected` (correct key but a different audience → 401), and `TokenSignedBySampleServer_IsAccepted` (a token signed with the SampleServer's own DI-resolved key, `iss=http://localhost`, `aud=OutbackAS` → not 401; the sample's `UserInfoAccessor` is a stub, so a non-401 status proves the token passed authentication). Verified the tests are not vacuous: temporarily setting `ValidAudience` to a wrong value made the positive test fail (401), confirming it genuinely depends on real audience validation; and with `ValidateIssuerSigningKey = false` the foreign-key token was still rejected because the handler validates the signature against the supplied key. Full regression: `Rinsen.Outback.Tests` 32 total with only the 4 known pre-existing FUNC-001/002/003 failures; `Rinsen.IdentityProvider.Tests` 15/15; `dotnet build Outback.slnx` 0 errors.
- **References:** ASP.NET Core `JwtBearerHandler`/`JwtBearerPostConfigureOptions` source; Microsoft identity guidance on explicit JWT validation

### SEC-012 · [P2] Use constant-time comparison for all secrets (4 sites)
- **Status:** Done (2026-11-24)
- **Location:** `ClientService.cs:47` · `AuthorizationCodeGrantTypeHandler.cs:139` · `LocalAccountService.cs:131` · `IdentityController.cs:249`
- **Problem:** `==` / `!=` / `SequenceEqual` short-circuit, leaking a timing side-channel.
- **Acceptance criteria:**
  - [x] All four sites use `CryptographicOperations.FixedTimeEquals` (length-checked)
  - [x] No remaining ordinal secret comparison in auth paths (grep)
  - [x] Tests: correct and incorrect secrets both produce the same comparison path
- **Resolution:** Added a shared `public static class SecretComparer` in `Rinsen.Outback` (`src/Outback/Rinsen.Outback/Helpers/SecretComparer.cs`, namespace `Rinsen.Outback.Helpers`) with two `FixedTimeEquals` overloads — `string` and `byte[]` — both delegating to `CryptographicOperations.FixedTimeEquals` (length-checked, no short-circuit) and failing closed on `null` inputs. It lives in `Rinsen.Outback` because all three consumer projects reference it (same public-static pattern as `InboundTokenValidation`/`HashHelper`). All four sites now call it: `ClientService.cs:47` (client secret vs stored hash), `AuthorizationCodeGrantTypeHandler.cs:139` (PKCE code challenge), `LocalAccountService.cs:131` (password hash, `byte[]` overload), `IdentityController.cs:249` (invite code vs config value).
  - **Grep verification:** no remaining `SequenceEqual` in `src/`; the only `==`/`!=` matches on secret-like names are benign (`existingSecret == default` is a null check on an entity; URL paths, public config values, booleans, DB lookups, and non-secret settings).
  - **Behavior note:** the `(null, null)` invite-code case now fails closed (rejected) where `null != null` previously was not — safer, edge case.
  - **Tests:** `tests/Rinsen.Outback.Tests/SecretComparerTests.cs` (new, 13 tests: equal/not-equal/different-length/null for both overloads, single-bit-flip case) and `tests/Rinsen.Outback.Tests/ClientServiceSecretValidationTests.cs` (new, 2 tests with a `FakeClientAccessor`: correct secret accepted, wrong secret → `SecurityException`). All 15 pass. Full regression: `Rinsen.Outback.Tests` 47 total with only the 4 known pre-existing FUNC-001/002/003 failures; `Rinsen.IdentityProvider.Tests` 15/15; `dotnet build Outback.slnx` 0 errors.

### SEC-013 · [P2] Store client secrets with a salted KDF
- **Status:** Done (2026-11-24)
- **Location:** `src/Outback/Rinsen.Outback/Helpers/HashHelper.cs` · `DefaultInstaller.cs:107,236` · `sample/.../ClientAccessor.cs:25,49`
- **Problem:** Unsalted SHA-256 is precomputable; a leaked `Secrets` table yields all secrets.
- **Acceptance criteria:**
  - [x] Replace with salted PBKDF2-HMAC-SHA256 (≥ 600k) or Argon2id; store per-client salt
  - [x] Provide a migration/rehash path for existing stored secrets
  - [x] Update `HashHelper`, `DefaultInstaller`, and the sample accessor
  - [x] Test: stored value is salted; verification still succeeds
- **Resolution:** `HashHelper` (in `Rinsen.Outback`, so all consumers share it) gained `HashClientSecret(string)` — PBKDF2-HMAC-SHA256 via `Rfc2898DeriveBytes.Pbkdf2` with a fresh 16-byte `RandomNumberGenerator` salt, `ClientSecretIterationCount = 600_000` (OWASP minimum), 32-byte output — returning a self-describing stored value `PBKDF2-SHA256$<iterations>$<base64url salt>$<base64url hash>` (the salt is stored per client, embedded in the value; the `OutbackClientSecrets.Secret` column is `nvarchar(max)`, no schema change needed). `VerifyClientSecret(secret, stored)` verifies both the new format (PBKDF2 with the stored salt, constant-time compare via `SecretComparer.FixedTimeEquals`) and the legacy unsalted SHA-256 format (kept as `GetSha256Hash`, now verification-only); malformed salted values fail closed (wrong part count, non-numeric/zero iterations, bad base64, empty salt/hash all → `false`), and null inputs fail closed. **Migration/rehash path:** new optional capability interface `IClientSecretRehasher` (`Rinsen.Outback/Accessors/`) with `RehashClientSecretAsync(clientId, oldStored, newStored)`; `ClientService.GetClient(ClientIdentity)` re-hashes a legacy stored secret with the salted KDF immediately after a successful verification (the only moment the plaintext is available) when the backing accessor implements the interface — failures are logged, never fail the token request. `Rinsen.IdentityProvider/Outback/ClientAccessor.cs` implements it against `OutbackDbContext.ClientSecrets` (matches on client + old stored value, excludes soft-deleted rows; the DbContext's `SaveChanges` override stamps `Updated`). All creation paths now store the salted format: `DefaultInstaller.cs:107,236` (both seeded clients), `Rinsen.IdentityProvider/Outback/ClientService.cs:347` (secret rotation), and the sample `ClientAccessor.cs:25,49` (so the SampleServer's `MachineToMachine`/`AuthorizationCodeGrant` flows exercise the new format end-to-end).
  - **Tests:** `tests/Rinsen.Outback.Tests/HashHelperClientSecretTests.cs` (new, 15 tests: salted format shape, two hashes of the same secret differ, correct/wrong secret against both formats, `IsLegacyClientSecret` both ways, 6 malformed-value fail-closed cases, null inputs) and `tests/Rinsen.Outback.Tests/ClientServiceSecretValidationTests.cs` (extended to 5 tests: salted stored secret accepted, wrong secret → `SecurityException`, legacy stored secret still accepted, legacy match triggers exactly one rehash call whose new value is salted and verifies, salted match triggers no rehash). Full regression: `Rinsen.Outback.Tests` 65/65; `Rinsen.IdentityProvider.Tests` 15/15; `dotnet build Outback.slnx` 0 errors.

### SEC-014 · [P2] Raise PBKDF2 iteration count to ≥ 600,000
- **Status:** Done (2026-11-24)
- **Location:** `src/OutbackApp/Rinsen.IdentityProvider/IdentityOptions.cs:7`
- **Problem:** 100,000 iterations is ~6× weaker than the OWASP minimum for PBKDF2-HMAC-SHA256.
- **Acceptance criteria:**
  - [x] Default `IterationCount = 600_000`
  - [x] Per-account `IterationCount` still honored for existing users
  - [x] Test: new account is created with the new default
- **Resolution:** `IdentityOptions.IterationCount` default raised from 100,000 to `600_000` (OWASP minimum for PBKDF2-HMAC-SHA256) in `IdentityOptions.cs`. No schema or migration change is needed: the per-account `IterationCount` is already persisted on `LocalAccounts.IterationCount` and is the value used at verification time — `LocalAccountService.GetPasswordHash` (line 79) derives with `localAccount.IterationCount`, and `UserService.cs:174` (password change) does the same, so existing users' stored hashes (created at any iteration count, including the legacy 100k) continue to validate exactly as before; only *new* accounts and *new* password hashes use the raised default. `Rinsen.IdentityProvider.csproj` gained `<InternalsVisibleTo Include="Rinsen.IdentityProvider.Tests" />` (mirroring `Rinsen.Outback.csproj`) so the test project can assert the internal `InvalidPasswordException` type.
  - **Tests:** `tests/Rinsen.IdentityProvider.Tests/LocalAccountServiceTests.cs` (new, 4 tests: default `IterationCount` ≥ 600,000; `CreateAsync` stores the new default on the account; an account created under the legacy 100k default still validates under a service configured with the new 600k default — proving verification is per-account; wrong password throws `InvalidPasswordException`). Fakes: `FakeIdentityAccessor` (fixed `IdentityId`, null `ClaimsPrincipal`) and `InMemoryLocalAccountStorage`. Full regression: `Rinsen.IdentityProvider.Tests` 19/19; `Rinsen.Outback.Tests` 65/65; `dotnet build Outback.slnx` 0 errors.
- **References:** OWASP Password Storage Cheat Sheet

### SEC-015 · [P3] Equalize login timing for unknown accounts
- **Status:** Open
- **Location:** `src/OutbackApp/Rinsen.IdentityProvider/LoginService.cs:60–64`
- **Problem:** Unknown accounts return before the KDF runs, enabling timing-based enumeration.
- **Acceptance criteria:**
  - [ ] On `AccountNotFoundException`, run a dummy PBKDF2 with the same cost
  - [ ] Response time for unknown vs. known account is statistically indistinguishable
  - [ ] Test asserts both paths perform a KDF

---

## Area 3 — Authentication & Login Flow Security

### SEC-016 · [P1] Enforce lockout and add rate limiting
- **Status:** Open
- **Location:** `LocalAccountService.cs:137–147` · `IdentityOptions.cs:10` · repo-wide (no `AddRateLimiter`)
- **Problem:** `FailedLoginCount` is incremented but never enforced; `MaxFailedLoginSleepCount` is dead code; no throttling on any credential endpoint.
- **Acceptance criteria:**
  - [ ] `FailedLoginCount >= MaxFailedLoginSleepCount` blocks login with the generic error
  - [ ] `AddRateLimiter` registered and applied to `/Identity/Login`, `/Identity/TwoFactor`, `/Identity/Create`, `/oauth/token`
  - [ ] 429 returned when the window limit is exceeded
  - [ ] Tests: N+1th bad password is blocked; rate limit returns 429

### SEC-017 · [P1] Enforce `IsDisabled` in the login path
- **Status:** Open
- **Location:** `src/OutbackApp/Rinsen.IdentityProvider/LoginService.cs:67,149–154`
- **Problem:** A disabled account still authenticates; only `Deleted` is checked.
- **Acceptance criteria:**
  - [ ] `LoginAsync` rejects when `localAccount.IsDisabled` (covers the TOTP branch too)
  - [ ] Returns the same generic failure result
  - [ ] Test: disabled account → login fails; enabled account → succeeds

### SEC-018 · [P1] Remove hardcoded secrets from the Aspire host & rotate
- **Status:** Open
- **Location:** `src/Aspire/AspireOutback.AppHost/Program.cs:3,17,18,25`
- **Problem:** SQL password, two login passwords, and the invitation code are committed to source.
- **Acceptance criteria:**
  - [ ] `AddParameter("sql-password")` has no default (prompt / user-secrets / env)
  - [ ] `Login__Debug__Password`, `Login__Runtime__Password`, `Rinsen__InvitationCode` sourced from configuration, not literals
  - [ ] **All four values rotated** (treat as compromised)
  - [ ] Add a secret scanner (e.g., gitleaks) to CI to prevent recurrence

### SEC-019 · [P1] Stop logging the client secret at install time
- **Status:** Open
- **Location:** `src/OutbackApp/Rinsen.Outback.App.Installation/Program.cs:71`
- **Problem:** The generated client secret is logged at Information (always enabled).
- **Acceptance criteria:**
  - [ ] Log only `ClientId`; never the secret
  - [ ] Grep confirms no other `ClientSecret`/secret value is logged
  - [ ] Test/log review: install output contains no secret

### SEC-020 · [P2] Strengthen the anonymous-registration invite code
- **Status:** Open
- **Location:** `IdentityController.cs:241–263` · `AspireOutback.AppHost/Program.cs:25`
- **Problem:** Unauthenticated account creation gated only by a 4-char code compared with `!=`, no rate limiting.
- **Acceptance criteria:**
  - [ ] Invite code is a strong random value (≥ 128 bits)
  - [ ] Compared with `CryptographicOperations.FixedTimeEquals`
  - [ ] Endpoint is rate-limited
  - [ ] Re-evaluate whether public self-registration should exist at all

### SEC-021 · [P3] Enforce `Url.IsLocalUrl` in all environments
- **Status:** Open
- **Location:** `src/OutbackApp/Rinsen.Outback.App/Controllers/IdentityController.cs:164–167`
- **Problem:** Development branch redirects to an unvalidated `returnUrl` (open redirect).
- **Acceptance criteria:**
  - [ ] `LoginSuccess` validates `returnUrl` with `Url.IsLocalUrl` in every environment
  - [ ] External `returnUrl` falls back to `~/`
  - [ ] Test: external URL is not honored

### SEC-022 · [P3] Two-factor session lookup must honor `Deleted` and age
- **Status:** Open
- **Location:** `TwoFactorAuthenticationSessionStorage.cs:29–40,89–115` · `LoginService.ConfirmTotpCode:122–143`
- **Problem:** A logically-deleted or arbitrarily old 2FA session can still be completed.
- **Acceptance criteria:**
  - [ ] `SELECT` filters `AND Deleted IS NULL`
  - [ ] `ConfirmTotpCode` rejects sessions past a maximum age
  - [ ] Tests: deleted session rejected; expired session rejected

### SEC-023 · [P3] Filter sensitive keys from the admin configuration dump
- **Status:** Open
- **Location:** `src/OutbackApp/Rinsen.Outback.App/ApiControllers/AdminController.cs:31–41`
- **Problem:** `GetDebugView()` exposes connection strings and env-supplied secrets to any admin.
- **Acceptance criteria:**
  - [ ] Filter out keys matching `connectionstring`, `password`, `secret`, `invitationcode` (case-insensitive)
  - [ ] Response no longer contains the DB connection string
  - [ ] Test asserts sensitive keys are absent

---

## Functional defects (context — not security-scored)

> These block the OAuth flows from working at all and should be tracked alongside the
> security items, but are not vulnerabilities in themselves.

### FUNC-001 · [P3] Register `IGrantTypeHandler` implementations in DI
- **Status:** Done (2026-11-24)
- **Location:** `src/Outback/Rinsen.Outback/ExtensionMethods.cs` (`AddServices`)
- **Problem:** `OAuthController`'s `IEnumerable<IGrantTypeHandler>` resolves empty; every grant path returns `invalid_request`.
- **Acceptance criteria:**
  - [x] Register `AuthorizationCodeGrantTypeHandler`, `RefreshTokenGrantTypeHandler`, `ClientCredentialsGrantTypeHandler`, `DeviceCodeGrantTypeHandler`
  - [x] Each grant type resolves to its handler at runtime
- **Resolution:** Registered all four handlers as `AddScoped<IGrantTypeHandler, …>` in `ExtensionMethods.AddServices` (the `Rinsen.Outback` library composition root, called by both `AddRinsenOutback` overloads). Both consuming apps pick them up without any local registration: `Rinsen.Outback.App` (`Program.cs` → `AddRinsenOutback()`) and `SampleServer` (`Startup.cs` → `AddRinsenOutback()`); grep confirms SampleServer performs no `IGrantTypeHandler` registration of its own, so there is no double-registration. `OAuthController`'s `IEnumerable<IGrantTypeHandler>` now resolves all four, and each grant type maps to its handler via `SingleOrDefault(h => h.GrantType == …)`. Verified at runtime: the previously failing `MachineToMachine` tests (client-credentials, BasicAuth + PostForm) and the `AuthorizationCodeGrant`/`DeviceAuthorizationFlow`/refresh-token tests all pass.

### FUNC-002 · [P3] Implement `ClientCredentialsGrantTypeHandler.GrantType`
- **Status:** Done (2026-11-24)
- **Location:** `src/Outback/Rinsen.Outback/Grants/ClientCredentialsGrantTypeHandler.cs:18`
- **Problem:** Getter throws `NotImplementedException`, breaking handler enumeration.
- **Acceptance criteria:**
  - [x] Return `"client_credentials"`
  - [x] Client-credentials token request succeeds end-to-end
- **Resolution:** `GrantType` now returns `"client_credentials"` (was `throw new NotImplementedException()`), so `OAuthController`'s handler enumeration no longer throws and the grant type matches the standard OAuth 2.0 client-credentials identifier. End-to-end verified by the `MachineToMachine` tests: `WhenUsingClientCredentialsInBasicAuthHeaderGetAccessToken` and `WhenUsingClientCredentialsInPostFormGetAccessToken` both succeed (200 + access token) against the SampleServer in `Rinsen.Outback.Tests`.

### FUNC-003 · [P3] Implement device-flow storage + consent
- **Status:** Open
- **Location:** `GrantAccessor.cs:54–57,139–142` · `DeviceController.cs` (consent POST commented out)
- **Problem:** Device-flow accessor methods throw; consent POST is disabled.
- **Acceptance criteria:**
  - [ ] `GetDeviceAuthorizationGrantAsync` / `SaveDeviceAuthorizationGrantAsync` implemented
  - [ ] Device consent POST re-enabled and wired
  - [ ] Device flow completes end-to-end

### FUNC-004 · [P3] Implement `LoginService.LogoutAsync`
- **Status:** Open
- **Location:** `src/OutbackApp/Rinsen.IdentityProvider/LoginService.cs:212–215`
- **Problem:** Throws `NotImplementedException`.
- **Acceptance criteria:**
  - [ ] Signs the user out and removes the server-side ticket
  - [ ] Test: session invalidated after logout

### FUNC-005 · [P3] Null-guard `AbnfValidationHelper.IsValid`
- **Status:** Open
- **Location:** `src/Outback/Rinsen.Outback/Helpers/AbnfValidationHelper.cs`
- **Problem:** `IsValid(value, min, max)` throws `NullReferenceException` on null (latent 500).
- **Acceptance criteria:**
  - [ ] Returns `false` for null/empty instead of throwing
  - [ ] Test: null input → `false`, no exception

---

*Generated from `security-audit-report.html` on 2026-11-24. No source modifications made.*
