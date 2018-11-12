using Server_Tools.Idrac.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tools.Idrac.Controllers
{
    class ChassisController : BaseIdrac
    {
        public const string ChassisRoot = @"/redfish/v1/Chassis/System.Embedded.1";

        public ChassisController(Server server)
            : base(server) { }

        /// <summary>
        /// Retorna as informações de Chassis do servidor
        /// </summary>
        /// <returns></returns>
        public async Task<Chassis> GetChassisInformation()
        {
            return await GetResource<Chassis>(BaseUri + ChassisRoot);
        }

    }
}
