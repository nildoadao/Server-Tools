using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
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
    }
}
