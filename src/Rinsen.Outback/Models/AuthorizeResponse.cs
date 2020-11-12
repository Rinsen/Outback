using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rinsen.Outback.Models
{
    public class AuthorizeResponse
    {
        public string Code { get; set; }

        public string Scope { get; set; }

        public string State { get; set; }

        public string SessionState { get; set; }

        public string FormPostUri { get; set; }

    }
}
