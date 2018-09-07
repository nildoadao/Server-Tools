using Newtonsoft.Json;
using Server_Tools.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Threading.Tasks;
namespace Server_Tools.Util
{
    class HttpUtil
    {
        private static HttpClient _client;
        private static HttpClientHandler _handler;

        #region Parametros default

        private const string IDRAC_USER = "root";
        private const string IDRAC_PASSWORD = "calvin";

        #endregion

        public static HttpClient GetClient()
        {
            if(_client == null)
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
                _handler = new HttpClientHandler();
                SecureString password = new SecureString();
                foreach (char c in IDRAC_PASSWORD.ToCharArray())
                {
                    password.AppendChar(c);
                }
                _handler.Credentials = new NetworkCredential(IDRAC_USER, password);
                _handler.PreAuthenticate = true;
                _client = new HttpClient(_handler);
                _client.DefaultRequestHeaders.Add("User-Agent", "Server Tools");
            }
            return _client;
        }
    }
}
