using Newtonsoft.Json;
using Server_Tools.Idrac.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tools.Idrac.Controllers
{
    class StorageController : BaseIdrac
    {
        public const string Controllers = @"/redfish/v1/Systems/System.Embedded.1/Storage";

        public StorageController(Server server)
            : base(server) { }

        /// <summary>
        /// Retorna uma lista com as localizações de todas as enclousures
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetEnclousuresLocation()
        {
            using(var request = new HttpRequestMessage(HttpMethod.Get, baseUri + Controllers))
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
        /// Retorna uma enclousure
        /// </summary>
        /// <param name="uri">Uri com a localização do recurso</param>
        /// <returns></returns>
        public async Task<Enclousure> GetEnclousure(string uri)
        {
            return await GetResource<Enclousure>(baseUri + uri);
        } 

        /// <summary>
        /// Retorna todas as enclousures intaladas no servidor
        /// </summary>
        /// <returns>Lista com todas enclousures</returns>
        public async Task<List<Enclousure>> GetAllEnclousures()
        {
            var locations = await GetEnclousuresLocation();
            var enclousures = new List<Enclousure>();
            foreach(var location in locations)
            {
                enclousures.Add(await GetEnclousure(location));
            }
            return enclousures;
        }

        /// <summary>
        /// Retorna uma controladora Raid
        /// </summary>
        /// <param name="uri">Localização do recurso</param>
        /// <returns>Objeto contendo a controladora</returns>
        public async Task<RaidController> GetRaidController(string uri)
        {
            return await GetResource<RaidController>(baseUri + uri);
        }

        /// <summary>
        /// Retorna todas as controladoras Raid instaladas no servidor
        /// </summary>
        /// <returns>Lista com controladoras</returns>
        public async Task<List<RaidController>> GetAllRaidControllers()
        {
            var enclousures = await GetAllEnclousures();
            var controllers = new List<RaidController>();
            foreach(var enclousure in enclousures)
            {
                controllers.AddRange(enclousure.StorageControllers);
            }
            return controllers;
        }

        /// <summary>
        /// Retorna todos os discos associados a uma determinada Controladora
        /// </summary>
        /// <param name="controller">Controladora que abriga os discos</param>
        /// <returns>Lista com todos os discos da controladora</returns>
        public async Task<List<PhysicalDisk>> GetPhysicalDisks(Enclousure controller)
        {
            List<PhysicalDisk> disks = new List<PhysicalDisk>();
            foreach(var item in controller.Drives)
            {
               disks.Add(await GetResource<PhysicalDisk>(baseUri + item.Id));
            }
            return disks;
        }

        /// <summary>
        /// Retorna os discos fisicos pertencentes a um disco virtual
        /// </summary>
        /// <param name="virtualDisk">Disco virtual</param>
        /// <returns>Lista com todos os discos fisicos</returns>
        public async Task<List<PhysicalDisk>> GetPhysicalDisks(VirtualDisk virtualDisk)
        {
            List<PhysicalDisk> disks = new List<PhysicalDisk>();
            foreach(var item in virtualDisk.Links.Drives)
            {
                disks.Add(await GetResource<PhysicalDisk>(baseUri + item.Id));
            }
            return disks;
        }

        /// <summary>
        /// Retorna todos os discos fisicos do servidor
        /// </summary>
        /// <returns>Lista com todos os discos</returns>
        public async Task<List<PhysicalDisk>> GetAllPhysicalDisks()
        {
            var disks = new List<PhysicalDisk>();
            foreach(var controller in await GetAllEnclousures())
            {
                disks.AddRange(await GetPhysicalDisks(controller));
            }
            return disks;
        }

        /// <summary>
        /// Retorna um disco virtual baseado na sua localização
        /// </summary>
        /// <param name="uri">Localização do recurso</param>
        /// <returns>Obejto contendo o disco virtual</returns>
        public async Task<VirtualDisk> GetVirtualDisk(string uri)
        {
            return await GetResource<VirtualDisk>(baseUri + uri);
        }

        /// <summary>
        /// Retorna todos os discos virtuais de uma enclousure
        /// </summary>
        /// <param name="enclousure">Enclousure onde estão os discos</param>
        /// <returns>Lista com todos os discos virtuais</returns>
        public async Task<List<VirtualDisk>> GetVirtualDisks(Enclousure enclousure)
        {
            string location = enclousure.Volumes.Id;
            using(var request = new HttpRequestMessage(HttpMethod.Get, baseUri + location))
            {
                request.Headers.Authorization = credentials;
                using(var response = await client.SendAsync(request))
                {
                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException(string.Format("Falha ao obter discos virtuais {0}", response.ReasonPhrase));

                    var volumesCollection = new
                    {
                        Members = new List<OdataObject>()
                    };
                    string jsonData = await response.Content.ReadAsStringAsync();
                    var collection = JsonConvert.DeserializeAnonymousType(jsonData, volumesCollection);
                    var virtualDisks = new List<VirtualDisk>();
                    foreach (var item in collection.Members)
                    {
                        virtualDisks.Add(await GetResource<VirtualDisk>(baseUri + item.Id));
                    }
                    return virtualDisks;
                }
            }
        }

        /// <summary>
        /// Retorna todos os discos virtuais presentes no servidor
        /// </summary>
        /// <returns>Lista com todos discos virtuais</returns>
        public async Task<List<VirtualDisk>> GetAllVirtualDisks()
        {
            var virtualDisks = new List<VirtualDisk>();
            foreach(var item in await GetAllEnclousures())
            {
                virtualDisks.AddRange(await GetVirtualDisks(item));
            }
            return virtualDisks;
        }

        /// <summary>
        /// Cria um disco virtual
        /// </summary>
        /// <param name="disks">Lista de disco do VD</param>
        /// <param name="enclousure">Controladora que gerencia o VD</param>
        /// <param name="level">Nivel do RAID</param>
        /// <param name="size">Tamanho do VD</param>
        /// <param name="stripeSize">Representa o OptimumIOSizeBytes</param>
        /// <param name="name">Nome do VD a ser criado</param>
        /// <returns>Job da operação</returns>
        public async Task<IdracJob> CreateVirtualDisk(List<PhysicalDisk> disks, Enclousure enclousure, string level, int size, int stripeSize, string name)
        {
            List<OdataObject> drives = new List<OdataObject>();
            foreach (var disk in disks)
            {
                drives.Add(new OdataObject() { Id = disk.OdataId });
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
            return await idrac.CreateJob(string.Format("{0}{1}/Volumes", baseUri, enclousure.OdataId), httpContent);
        }

        public async Task<IdracJob> CreateVirtualDisk(List<PhysicalDisk> disks, Enclousure enclousure, string level)
        {
            List<OdataObject> drives = new List<OdataObject>();
            foreach (var disk in disks)
            {
                drives.Add(new OdataObject() { Id = disk.OdataId });
            }
            var content = new
            {
                VolumeType = level,
                Drives = drives,
            };
            var jsonContent = JsonConvert.SerializeObject(content);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var idrac = new JobController(server);
            return await idrac.CreateJob(string.Format("{0}{1}/Volumes", baseUri, enclousure.OdataId), httpContent);
        }


        public async Task<IdracJob> CreateVirtualDisk(List<PhysicalDisk> disks, Enclousure enclousure, string level, string name)
        {
            List<OdataObject> drives = new List<OdataObject>();
            foreach (var disk in disks)
            {
                drives.Add(new OdataObject() { Id = disk.OdataId });
            }
            var content = new
            {
                VolumeType = level,
                Drives = drives,
                Name = name,
            };
            var jsonContent = JsonConvert.SerializeObject(content);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var idrac = new JobController(server);
            return await idrac.CreateJob(string.Format("{0}{1}/Volumes", baseUri, enclousure.OdataId), httpContent);
        }

        /// <summary>
        /// Deleta um disco virtual
        /// </summary>
        /// <param name="virtualDisk">Objeto contendo o disco virtual</param>
        /// <returns>Job de exclusão do VD</returns>
        public async Task<IdracJob> DeleteVirtualDisk(VirtualDisk virtualDisk)
        {
            var httpContent = new StringContent("", Encoding.UTF8, "application/json");
            var idrac = new JobController(server);
            return await idrac.CreateJob(baseUri + virtualDisk.OdataId.Id, HttpMethod.Delete);
        }
    }
}
