using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;

namespace APKScanWeb.Models
{
    public class VMControl
    {
        
        private class AccessTicket
        {
            public class Data
            {
                public string CSRFPreventionToken { get; set; }
                public string Ticket { get; set; }
            }
            public Data data  { get; set; }
        }
        //----------------------------------------------------------------------------------------------------
        public enum eAuthMethods { pve, pam };
        //----------------------------------------------------------------------------------------------------
        private static object lockObj = new object();
        private static VMControl singleton = null;
        private static string CSRFToken { get; set; }
        private static string PVEAuthCookie { get; set; }
        private static string serverUrl { get; set; }
        private static HttpClient client;
        private static string loginUsername;
        private static string loginPassword;
        public static eAuthMethods authMethod;
        //----------------------------------------------------------------------------------------------------
        public static VMControl getInstance(string url, string username, string password, eAuthMethods auth)
        {
            if (singleton == null)
            {
                lock (lockObj)
                {
                    if (singleton == null)
                        return singleton = new VMControl(url, username, password, auth);
                    else
                        return singleton;
                }
            }
            else
                return singleton;
        }
        //only used after the one with parameters is called...
        public static VMControl getInstance()
        {
            if (singleton == null)
            {
                lock (lockObj)
                {
                    if (singleton == null)
                        throw new Exception("Can't call this method now, first you need to call the one with parameters!");
                    else
                        return singleton;
                }
            }
            else
                return singleton;
        }
        //----------------------------------------------------------------------------------------------------
        private async Task<bool> checkIfLoggedIn()
        {

            
            return false;
        }
        //----------------------------------------------------------------------------------------------------
        private async Task<bool> loginToServer()
        {
            AccessTicket authTicket = null;
            var data = new { username = loginUsername, password = loginPassword, realm = authMethod == eAuthMethods.pam ? "pam" : "pve" };

            using (client)
            {
                var response = client.PostAsJsonAsync(serverUrl + "api2/json/access/ticket", data).Result;

                if (response.IsSuccessStatusCode)
                {
                    authTicket = response.Content.ReadAsAsync<AccessTicket>().Result;
                    if (authTicket != null)
                    {
                        if (authTicket.data == null)
                            return false;

                        if (authTicket.data.CSRFPreventionToken == null || authTicket.data.Ticket == null)
                            return false;

                        //init the class set the global static variables
                        CSRFToken = authTicket.data.CSRFPreventionToken;
                        PVEAuthCookie = authTicket.data.Ticket;

                        return true;
                    }
                }

            }
            return false;
        }
        //----------------------------------------------------------------------------------------------------
        private VMControl(string url, string username, string password, eAuthMethods auth)
        {
            serverUrl = url;
            client = new HttpClient();
            client.BaseAddress = new Uri(serverUrl);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            loginPassword = password;
            loginUsername = username;
            authMethod = auth;


            var x = loginToServer();

            if (!x.Result)
                throw new Exception("Error while trying to connect to the server, check your configuration!");

        }
        //----------------------------------------------------------------------------------------------------
    }
}
