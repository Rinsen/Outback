﻿using System;

namespace Rinsen.Outback.Grants;

public class CodeGrant
{
    public string ClientId { get; set; } = string.Empty;

    public string SubjectId { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string CodeChallenge { get; set; } = string.Empty;

    public string CodeChallengeMethod { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string Nonce { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;

    public DateTimeOffset Created { get; set; }

    public DateTimeOffset Expires { get; set; }
}
