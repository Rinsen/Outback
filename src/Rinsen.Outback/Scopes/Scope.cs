﻿using System.Collections.Generic;

namespace Rinsen.Outback.Scopes
{
    public class Scope
    {
        public string ScopeName { get; set; }

        public bool ShowInDiscoveryDocument { get; set; }

        public List<ScopeClaim> Claims { get; set; }
    }
}
