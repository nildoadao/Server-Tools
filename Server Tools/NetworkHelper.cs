using System;
using System.Net.NetworkInformation;

namespace Server_Tools
{
    class NetworkHelper
    {
        //Realiza um teste de ping para um determinado IP
        public static bool IsConnected(string IpAddres)
        {
            try
            {
                Ping ping = new Ping();
                PingReply reply = ping.Send(IpAddres);

                if (reply.Status == IPStatus.Success)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception) // Caso o endereço seja fornecido de maneira errada/ mal formatado
            {
                return false;
            }
        }
    }
}
