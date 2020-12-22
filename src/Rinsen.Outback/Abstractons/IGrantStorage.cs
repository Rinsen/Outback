using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rinsen.Outback.Grants;

namespace Rinsen.Outback.Abstractons
{
    public interface IGrantStorage
    {
        Task<Grant> GetGrant(string code);
        Task Save(Grant grant);
    }
}
