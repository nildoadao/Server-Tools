﻿using Newtonsoft.Json;
using Server_Tools.Idrac.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Server_Tools.Idrac.Controllers
{
    class StorageController : BaseIdrac
    {
        // Uri para acesso a Controladora Raid
        public const string Controllers = @"/redfish/v1/Systems/System.Embedded.1/Storage";

        /// <summary>
        /// Cria uma nova instancia de StorageController
        /// </summary>
        /// <param name="server">Objeto contendo os dados basicos do servidor</param>
        public StorageController(Server server)
            : base(server) { }

        /// <summary>
        /// Retorna uma lista com as localizações de todas as enclousures
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetEnclousuresLocationAsync()
        {
            using(var request = new HttpRequestMessage(HttpMethod.Get, BaseUri + Controllers))
            {
                request.Headers.Authorization = Credentials;
                using(var response = await Client.SendAsync(request))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        throw new UnauthorizedAccessException("Acesso negado, verifique usuario/senha");

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
        public async Task<Enclousure> GetEnclousureAsync(string uri)
        {
            return await GetResourceAsync<Enclousure>(BaseUri + uri);
        } 

        /// <summary>
        /// Retorna todas as enclousures intaladas no servidor
        /// </summary>
        /// <returns>Lista com todas enclousures</returns>
        public async Task<List<Enclousure>> GetAllEnclousuresAsync()
        {
            var locations = await GetEnclousuresLocationAsync();
            var enclousures = new List<Enclousure>();
            foreach(var location in locations)
            {
                enclousures.Add(await GetEnclousureAsync(location));
            }
            return enclousures;
        }

        /// <summary>
        /// Retorna uma controladora Raid
        /// </summary>
        /// <param name="uri">Localização do recurso</param>
        /// <returns>Objeto contendo a controladora</returns>
        public async Task<RaidController> GetRaidControllerAsync(string uri)
        {
            return await GetResourceAsync<RaidController>(BaseUri + uri);
        }

        /// <summary>
        /// Retorna todas as controladoras Raid instaladas no servidor
        /// </summary>
        /// <returns>Lista com controladoras</returns>
        public async Task<List<RaidController>> GetAllRaidControllersAsync()
        {
            var enclousures = await GetAllEnclousuresAsync();
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
        /// <param name="enclousure">Controladora que abriga os discos</param>
        /// <returns>Lista com todos os discos da controladora</returns>
        public async Task<List<PhysicalDisk>> GetPhysicalDisksAsync(Enclousure enclousure)
        {
            if (enclousure == null)
                throw new ArgumentNullException("enclousure", "O argumento não pode ser nulo");

            List<PhysicalDisk> disks = new List<PhysicalDisk>();
            foreach(var item in enclousure.Drives)
            {
               disks.Add(await GetResourceAsync<PhysicalDisk>(BaseUri + item.Id));
            }
            return disks;
        }

        /// <summary>
        /// Retorna os discos fisicos pertencentes a um disco virtual
        /// </summary>
        /// <param name="volume">Disco virtual</param>
        /// <returns>Lista com todos os discos fisicos</returns>
        public async Task<List<PhysicalDisk>> GetPhysicalDisksAsync(VirtualDisk volume)
        {
            if (volume == null)
                throw new ArgumentNullException("volume", "O argumento não pode ser nulo");

            List<PhysicalDisk> disks = new List<PhysicalDisk>();
            foreach(var item in volume.Links.Drives)
            {
                disks.Add(await GetResourceAsync<PhysicalDisk>(BaseUri + item.Id));
            }
            return disks;
        }

        /// <summary>
        /// Retorna todos os discos fisicos do servidor
        /// </summary>
        /// <returns>Lista com todos os discos</returns>
        public async Task<List<PhysicalDisk>> GetAllPhysicalDisksAsync()
        {
            var disks = new List<PhysicalDisk>();
            foreach(var controller in await GetAllEnclousuresAsync())
            {
                disks.AddRange(await GetPhysicalDisksAsync(controller));
            }
            return disks;
        }

        /// <summary>
        /// Retorna um disco virtual baseado na sua localização
        /// </summary>
        /// <param name="uri">Localização do recurso</param>
        /// <returns>Obejto contendo o disco virtual</returns>
        public async Task<VirtualDisk> GetVirtualDiskAsync(string uri)
        {
            return await GetResourceAsync<VirtualDisk>(BaseUri + uri);
        }

        /// <summary>
        /// Retorna todos os discos virtuais de uma enclousure
        /// </summary>
        /// <param name="enclousure">Enclousure onde estão os discos</param>
        /// <returns>Lista com todos os discos virtuais</returns>
        public async Task<List<VirtualDisk>> GetVirtualDisksAsync(Enclousure enclousure)
        {
            if (enclousure == null)
                throw new ArgumentNullException("enclousure", "O argumento não pode ser nulo");

            string location = enclousure.Volumes.Id;
            using(var request = new HttpRequestMessage(HttpMethod.Get, BaseUri + location))
            {
                request.Headers.Authorization = Credentials;
                using(var response = await Client.SendAsync(request))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        throw new UnauthorizedAccessException("Acesso negado, verifique usuario/senha");

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
                        virtualDisks.Add(await GetResourceAsync<VirtualDisk>(BaseUri + item.Id));
                    }
                    return virtualDisks;
                }
            }
        }

        /// <summary>
        /// Retorna todos os discos virtuais presentes no servidor
        /// </summary>
        /// <returns>Lista com todos discos virtuais</returns>
        public async Task<List<VirtualDisk>> GetAllVirtualDisksAsync()
        {
            var virtualDisks = new List<VirtualDisk>();
            foreach(var item in await GetAllEnclousuresAsync())
            {
                virtualDisks.AddRange(await GetVirtualDisksAsync(item));
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
        public async Task<IdracJob> CreateVirtualDiskAsync(List<PhysicalDisk> disks, Enclousure enclousure, string level, long size, long stripeSize, string name)
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
            var idrac = new JobController(Server);
            return await idrac.CreateJobAsync(string.Format("{0}{1}/Volumes", BaseUri, enclousure.OdataId), httpContent);
        }

        /// <summary>
        /// Cria um disco virtual
        /// </summary>
        /// <param name="disks">Lista de discos fisicos</param>
        /// <param name="enclousure">Enclousure responsável pelos discos</param>
        /// <param name="level">Nivel de Raid</param>
        /// <returns></returns>
        public async Task<IdracJob> CreateVirtualDiskAsync(List<PhysicalDisk> disks, Enclousure enclousure, string level)
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
            var idrac = new JobController(Server);
            return await idrac.CreateJobAsync(string.Format("{0}{1}/Volumes", BaseUri, enclousure.OdataId), httpContent);
        }

        /// <summary>
        /// Cria um disco virtual
        /// </summary>
        /// <param name="disks">Lsta de discos fisicos</param>
        /// <param name="enclousure">Enclousure resposavel pelos discos</param>
        /// <param name="level">Nivel de Raid</param>
        /// <param name="name">Nome do disco virtual</param>
        /// <returns></returns>
        public async Task<IdracJob> CreateVirtualDiskAsync(List<PhysicalDisk> disks, Enclousure enclousure, string level, string name)
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
            var idrac = new JobController(Server);
            return await idrac.CreateJobAsync(string.Format("{0}{1}/Volumes", BaseUri, enclousure.OdataId), httpContent);
        }

        /// <summary>
        /// Deleta um disco virtual
        /// </summary>
        /// <param name="volume">Objeto contendo o disco virtual</param>
        /// <returns>Job de exclusão do VD</returns>
        public async Task<IdracJob> DeleteVirtualDiskAsync(VirtualDisk volume)
        {
            if (volume == null)
                throw new ArgumentNullException("volume", "O argumento não pode ser nulo");

            var idrac = new JobController(Server);
            return await idrac.CreateJobAsync(BaseUri + volume.OdataId, HttpMethod.Delete);
        }
    }
}
