using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Server_Tools.Util
{
    class NetworkHelper
    {
        /// <summary>
        /// Checa se um determinado IP está respondendo na rede
        /// </summary>
        /// <param name="IpAddres">String contendo o IP</param>
        /// <returns></returns>
        public static bool IsConnected(string IpAddress)
        {
            try
            {
                Ping ping = new Ping();
                PingReply reply = ping.Send(IpAddress);
                if (reply.Status == IPStatus.Success)
                    return true;
                else
                    return false;
            }
            catch (Exception) // Caso o endereço seja fornecido de maneira errada / mal formatado
            {
                return false;
            }
        }

        public static async Task<bool> CheckConnectionAsync(string host)
        {
            bool connection = false;
            await Task.Run(() =>
            {               
                try
                {
                    Ping ping = new Ping();
                    PingReply reply = ping.Send(host);
                    if (reply.Status == IPStatus.Success)
                        connection = true;
                    else
                        connection = false;
                }
                catch (Exception) // Caso o endereço seja fornecido de maneira errada / mal formatado
                {
                    connection = false;
                }
            });
            return connection;
        }
    }
}
