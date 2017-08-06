using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.XPath;

namespace APC_Notifications_Queue_Viewer
{
    public class InforTasks
    {
        public static byte[] AdditionalEntropy = { 1, 3, 4, 7, 8 };

        public static void SecureCreds(string username, string apiToken)
        {
            byte[] utf8Creds = UTF8Encoding.UTF8.GetBytes(username + ":" + apiToken);

            byte[] securedCreds = null;

            // Encrypt credentials
            try
            {
                securedCreds = ProtectedData.Protect(utf8Creds, AdditionalEntropy, DataProtectionScope.CurrentUser);

                // Check if registry path exists
                if (CheckOrCreateRegPath())
                {
                    // Save encrypted key to registry
                    RegistryKey credsKey = Registry.CurrentUser.OpenSubKey(@"Software\Swiftpage Support\Infor Logins", true);
                    credsKey.SetValue("Infor Login", securedCreds);
                }
            }
            catch (CryptographicException e)
            {
                MessageBox.Show("Unable to encrypt Infor login credentials:\n\n" + e.ToString());
            }
        }

        public static byte[] UnsecureCreds()
        {
            // Check if registry path exists
            if (CheckOrCreateRegPath())
            {
                byte[] securedCreds = null;
                byte[] utf8Creds = null;

                // Get encrypted key from registry
                try
                {
                    RegistryKey jenkinsCredsKey = Registry.CurrentUser.OpenSubKey(@"Software\Swiftpage Support\Infor Logins", false);
                    securedCreds = (byte[])jenkinsCredsKey.GetValue("Infor Login");

                    // Un-encrypt credentials
                    try
                    {
                        utf8Creds = ProtectedData.Unprotect(securedCreds, AdditionalEntropy, DataProtectionScope.CurrentUser);
                    }
                    catch (CryptographicException e)
                    {
                        MessageBox.Show("Unable to unencrypt Infor login credentials:\n\n" + e.ToString());
                    }
                }
                catch (Exception error)
                {
                    MessageBox.Show("Unable to get stored Infor credentials\n\n" + error.Message);
                }

                return utf8Creds;
            }
            return null;
        }

        /// <summary>
        /// Verifies that the registry key to store Jenkins credentials exists, and creates it if not
        /// </summary>
        /// <returns>true if key is now created and valid, false if not</returns>
        public static bool CheckOrCreateRegPath()
        {
            RegistryKey key = null;

            // Check if subkey "HKCU\Software\Swiftpage Support" exists
            key = Registry.CurrentUser.OpenSubKey(@"Software\Swiftpage Support", false);
            if (key == null)
            {
                try
                {
                    key = Registry.CurrentUser.OpenSubKey(@"Software", true);
                    key.CreateSubKey("Swiftpage Support");
                }
                catch
                {
                    return false;
                }
            }

            // Check if subkey HKCU\Software\Swiftpage Support\JenkinsLogins exists
            key = Registry.CurrentUser.OpenSubKey(@"Software\Swiftpage Support\Infor Logins", false);
            if (key == null)
            {
                try
                {
                    key = Registry.CurrentUser.OpenSubKey(@"Software\Swiftpage Support", true);
                    key.CreateSubKey("Infor Logins");
                }
                catch
                {
                    return false;
                }
            }

            // Confirm that full subkey exists
            key = Registry.CurrentUser.OpenSubKey(@"Software\Swiftpage Support\Infor Logins", false);
            if (key != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static async Task<HttpResponseMessage> InforGetRequest(string baseUrl, string request)
        {
            // Create HttpClient with base URL
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(baseUrl);

            // Adding accept header for XML format
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

            // Getting the encrypted authentication details
            byte[] creds = UnsecureCreds();

            // If no authentication details, return blank message with Unauthorized status code
            if (creds == null)
            {
                HttpResponseMessage blankResponse = new HttpResponseMessage();
                blankResponse.StatusCode = System.Net.HttpStatusCode.Unauthorized;

                return blankResponse;
            }
            else
            {
                // Add authentication details to HTTP request
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(creds));

                // Run a Get request with the provided request path
                HttpResponseMessage response = new HttpResponseMessage();
                try
                {
                    response = await client.GetAsync(request);
                }
                catch (Exception error)
                {
                    MessageBox.Show("GET request failed in 'inforGetRequest(" + baseUrl + request + ")'.\n\n" + error);

                    HttpResponseMessage blankResponse = new HttpResponseMessage();
                    blankResponse.StatusCode = System.Net.HttpStatusCode.Unauthorized;

                    return blankResponse;
                }

                return response;
            }
        }

        public static async Task<XmlDocument> RunInforGet(string baseUrl, string request)
        {
            // Post a GET request to Infor and wait for a response
            HttpResponseMessage getRequest = await InforGetRequest(baseUrl, request);

            if (!getRequest.IsSuccessStatusCode)
            {
                MessageBox.Show(getRequest.ReasonPhrase);
                return null;
            }

            XmlDocument xmlOutput = new XmlDocument();
            xmlOutput.LoadXml(await getRequest.Content.ReadAsStringAsync());

            return xmlOutput;
        }

        public static async Task<List<Ticket>> GetTickets(string baseUrl, string request)
        {
            XmlDocument ticketResultDoc = await RunInforGet(baseUrl, request);
            if (ticketResultDoc == null)
            {
                return null;
            }

            MessageBox.Show(ticketResultDoc.OuterXml);
            List<Ticket> ticketList = new List<Ticket>();

            XmlNamespaceManager ticketNamespaceManager = new XmlNamespaceManager(ticketResultDoc.NameTable);
            XPathNavigator rootNode = ticketResultDoc.CreateNavigator();
            rootNode.MoveToFollowing(XPathNodeType.Element);
            IDictionary<string, string> xmlNamespaces = rootNode.GetNamespacesInScope(XmlNamespaceScope.All);
            MessageBox.Show(xmlNamespaces.Count.ToString());
            foreach (KeyValuePair<string, string> kvp in xmlNamespaces)
            {
                ticketNamespaceManager.AddNamespace(kvp.Key, kvp.Value);
            }
            if (!ticketNamespaceManager.HasNamespace("slx"))
            {
                ticketNamespaceManager.AddNamespace("slx", "http://schemas.sage.com/dynamic/2007");
            }

            XmlNodeList ticketNodes = ticketResultDoc.SelectNodes("//sdata:payload/slx:Ticket", ticketNamespaceManager);
            foreach (XmlNode ticketNode in ticketNodes)
            {
                Ticket ticket = new Ticket()
                {
                    idNumber = ticketNode.SelectSingleNode("slx:TicketNumber", ticketNamespaceManager).InnerText,
                    details = ticketNode.SelectSingleNode("slx:TicketProblem/slx:Notes", ticketNamespaceManager).InnerText,
                    subject = ticketNode.SelectSingleNode("slx:Subject", ticketNamespaceManager).InnerText,
                    additionalInfo = ticketNode.SelectSingleNode("slx:AdditionalInfo").InnerText
                };
                ticketList.Add(ticket);
            }

            return ticketList;
        }
    }

    public class Ticket
    {
        public string idNumber { get; set; }
        public string subject { get; set; }
        public string details { get; set; }
        public string additionalInfo { get; set; }
    }
}