using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace DotStd
{
    public class GmailMessage
    {
        public string title { get; set; }
        public string summary { get; set; }
    }

    public class GmailReader
    {
        // Reading Gmail in-box messages via atom feed interface.
        // https://mail.google.com/mail/feed/atom
        // https://stackoverflow.com/questions/7056715/reading-emails-from-gmail-in-c-sharp/19570553#19570553

        public string Email { get; set; }
        public string Password { get; set; }

        public List<GmailMessage> ReadMessages()
        {
            // get responses back from the free SMS email gateway.

            try
            {
                // Logging in Gmail server to get data
                using (var objClient = new System.Net.WebClient())
                {
                    objClient.Credentials = new System.Net.NetworkCredential(Email, Password);

                    // reading data and converting to string
                    var respRaw = objClient.DownloadData(@"https://mail.google.com/mail/feed/atom");
                    string response = Encoding.UTF8.GetString(respRaw);
                    response = response.Replace(@"<feed version=""0.3"" xmlns=""http://purl.org/atom/ns#"">", @"<feed>");

                    // loading into an XML so we can get information easily
                    // Creating a new xml document
                    var doc = new XmlDocument();
                    doc.LoadXml(response);

                    // nr of emails
                    var nr = doc.SelectSingleNode(@"/feed/fullcount").InnerText;

                    // Reading the title and the summary for every email
                    var msgs = new List<GmailMessage>();
                    foreach (XmlNode node in doc.SelectNodes(@"/feed/entry"))
                    {
                        msgs.Add(new GmailMessage
                        {
                            title = node.SelectSingleNode("title").InnerText,
                            summary = node.SelectSingleNode("summary").InnerText,
                        });
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
