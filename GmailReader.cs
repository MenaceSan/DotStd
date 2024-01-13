using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DotStd
{
    public class GmailMessage
    {
#pragma warning disable IDE1006 // Naming Styles
        public string? title { get; set; }
        public string? summary { get; set; }
#pragma warning restore IDE1006 // Naming Styles
    }

    /// <summary>
    /// Reading Gmail in-box messages via atom feed interface.
    /// https://mail.google.com/mail/feed/atom
    /// https://stackoverflow.com/questions/7056715/reading-emails-from-gmail-in-c-sharp/19570553#19570553
    /// </summary>
    public class GmailReader : ExternalService
    {
        public System.Net.NetworkCredential? _Credentials;   // more secure to store like this.

        public override string Name => "GmailReader";
        public override string BaseURL => "https://mail.google.com/mail/feed/atom";
        public override string Icon => "<i class='fab fa-google'></i>";


        /// <summary>
        /// Store my credentials. SecureString
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        public void SetCreds(string email, string password)
        {
            _Credentials = new System.Net.NetworkCredential(email, password);
        }

        /// <summary>
        /// get responses back from the free email gateway.
        /// </summary>
        /// <returns></returns>
        public async Task<List<GmailMessage>?> ReadMessages()
        {
            try
            {
                var httpClientHandler = new HttpClientHandler()
                {
                    Credentials = _Credentials,
                };

                UpdateTry();

                // Logging in Gmail server to get data
                using (var client = new HttpClient(httpClientHandler))
                {
                    // reading data and converting to string
                    byte[] respRaw = await client.GetByteArrayAsync(BaseURL);
                    string response = Encoding.UTF8.GetString(respRaw);
                    response = response.Replace(@"<feed version=""0.3"" xmlns=""http://purl.org/atom/ns#"">", @"<feed>");

                    // loading into an XML so we can get information easily
                    // Creating a new xml document
                    var doc = new XmlDocument();
                    doc.LoadXml(response);

                    // nr of emails
                    // string? nr = doc.SelectSingleNode(@"/feed/fullcount")?.InnerText;

                    // Reading the title and the summary for every email
                    var msgs = new List<GmailMessage>();
                    var entries = doc.SelectNodes(@"/feed/entry");
                    if (entries != null)
                    {
                        foreach (XmlNode node in entries)
                        {
                            msgs.Add(new GmailMessage
                            {
                                title = node.SelectSingleNode("title")?.InnerText,
                                summary = node.SelectSingleNode("summary")?.InnerText,
                            });
                        }
                    }

                    return msgs;
                }
            }
            catch // (Exception ex)
            {
                // MessageBox.Show("Check your network connection");
                return null;
            }
        }
    }
}
