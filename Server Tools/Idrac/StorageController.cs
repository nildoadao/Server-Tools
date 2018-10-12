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
        public const string DRIVES = @"/redfish/v1/Systems/System.Embedded.1/Storage/Drives/";
        public const string VOLUMES = @"/redfish/v1/Systems/System.Embedded.1/Storage/Volumes/";

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
                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException(string.Format("Falha ao listar controladoras: {0}", response.ReasonPhrase));

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
        public async Task<IdracStorageController> GetRaidController(string uri)
        {
            return await GetResource<IdracStorageController>(baseUri + uri);
        } 

        /// <summary>
        /// Retorna todos os discos associados a uma determinada Controladora
        /// </summary>
        /// <param name="controller">Controladora que abriga os discos</param>
        /// <returns>Lista com todos os discos da controladora</returns>
        public async Task<List<IdracPhysicalDisk>> GetPhysicalDisks(IdracStorageController controller)
        {
            List<IdracPhysicalDisk> disks = new List<IdracPhysicalDisk>();
            foreach(var item in controller.Drives)
            {
               disks.Add(await GetResource<IdracPhysicalDisk>(baseUri + item.Id));
            }
            return disks;
        }

        /// <summary>
        /// Cria um disco virtual
        /// </summary>
        /// <param name="disks">Lista de disco do VD</param>
        /// <param name="controller">Controladora que gerencia o VD</param>
        /// <param name="level">Nivel do RAID</param>
        /// <param name="size">Tamanho do VD</param>
        /// <param name="stripeSize">Representa o OptimumIOSizeBytes</param>
        /// <param name="name">Nome do VD a ser criado</param>
        /// <returns>Job da operação</returns>
        public async Task<IdracJob> CreateVirtualDisk(List<IdracPhysicalDisk> disks, IdracStorageController controller, int level, int size, int stripeSize, string name)
        {
            List<OdataObject> drives = new List<OdataObject>();
            foreach(var disk in disks)
            {
                drives.Add(new OdataObject { Id = disk.Id });
            }
            var content = new
            {
                VolumeType = level,
                Drives = drives,
                CapacityBytes = size,
                OptimumIOSizeBytes = stripeSize,
                Name = name
            };
            var jsonContent = JsonConvert.SerializeObject(content);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var idrac = new JobController(server);
            var jobId = await idrac.CreateJob(baseUri + DRIVES, httpContent);
            return await idrac.GetJob(jobId);
        }

        /// <summary>
        /// Deleta um disco virtual
        /// </summary>
        /// <param name="virtualDisk">Objeto contendo o disco virtual</param>
        /// <returns>Job de exclusão do VD</returns>
        public async Task<IdracJob> DeleteVirtualDisk(IdracVirtualDisk virtualDisk)
        {
            var httpContent = new StringContent("", Encoding.UTF8, "application/json");
            var idrac = new JobController(server);
            var jobId = await idrac.CreateJob(baseUri + VOLUMES + virtualDisk.Location.Id, httpContent, HttpMethod.Delete);
            return await idrac.GetJob(jobId);
        }
    }
}
