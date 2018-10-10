using Newtonsoft.Json;
using Server_Tools.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tools.Idrac
{
    class StorageController : BaseIdrac
    {
        public const string CONTROLLERS = @"/redfish/v1/Systems/System.Embedded.1/Storage";

        public StorageController(Server server)
            : base(server) { }

        /// <summary>
        /// Retorna uma lista com as localizações de todas as controladoras
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetControllersLocation()
        {
            using(var request = new HttpRequestMessage(HttpMethod.Get, baseUri + CONTROLLERS))
            {
                request.Headers.Authorization = credentials;
                using(var response = await client.SendAsync(request))
                {
                    var jsonData = await response.Content.ReadAsStringAsync();
                    var storageEntities = new
                    {
                        Members = new List<OdataObject>()
                    };
                    var entities = JsonConvert.DeserializeAnonymousType(jsonData, storageEntities);
                    var locations = new List<string>();
                    foreach(var location in entities.Members)
                    {
                        locations.Add(location.Id);
                    }
                    return locations;
                }
            }
        }

        /// <summary>
        /// Retorna uma controladora Raid
        /// </summary>
        /// <param name="uri">Uri com a localização do recurso</param>
        /// <returns></returns>
        public async Task<IdracRaidController> GetRaidController(string uri)
        {
            return await GetResource<IdracRaidController>(baseUri + uri);
        } 

        /// <summary>
        /// Retorna todos os discos associados a uma determinada Controladora
        /// </summary>
        /// <param name="controller">Controladora que abriga os discos</param>
        /// <returns>Lista com todos os discos da controladora</returns>
        public async Task<List<IdracPhysicalDisk>> GetPhysicalDisks(IdracRaidController controller)
        {
            List<IdracPhysicalDisk> disks = new List<IdracPhysicalDisk>();
            foreach(var item in controller.Drives)
            {
               disks.Add(await GetResource<IdracPhysicalDisk>(baseUri + item.Id));
            }
            return disks;
        }

    }
}
