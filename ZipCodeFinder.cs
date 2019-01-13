using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace DotStd
{
    [Serializable]
    public class ZipCode1
    {
        // A zip code decodes to this. for use with GeoLocation.
        // Poco - don't collide with the EF version of this. Use "ZipCode1" name.
        // https://gis.stackexchange.com/questions/53918/determining-which-us-zipcodes-map-to-more-than-one-state-or-more-than-one-city

        public string Zip { get; set; } // PK ? Zip can have multiple cities!
        public string City { get; set; }        // Maybe all caps ? "ALLSTON"
        public string State { get; set; }           // 2 letter code "MA" (can be added to cache dynamically)
        public string CountryCode { get; set; }     // 3 letter code. "USA" (MUST exist in country db)
    }

    [DataContract]
    public class ZipCodeZT
    {
        // Poco for ZT
        [DataMember] public string country { get; set; } // 2 letter code! US
        [DataMember] public string state { get; set; }   // MA
        [DataMember] public string city { get; set; }    // ALSTON
    }

    public class ZipCodeFinder
    {
        //! Call Web Service to look up a zip code. Maybe call IsValidZipCode() first ?
        //! if a zip code is not in my local cache i can look it up here.
        //! compliments GeoLocation
        // https://stackoverflow.com/questions/7129313/zip-code-lookup-api
        // https://www.melissa.com/lookups/ZipCityPhone.asp?InData=02134

        public string m_sResponse;  // Raw response.
        public ZipCode1 m_z;        // parsed m_sResponse.

        private static async Task<string> RequestStringAsync(string sReqUrl)
        {
            // Blocking call to get the string response to a HTTP query.

            var oRequest = WebRequest.Create(sReqUrl);

            var oResponse = await oRequest.GetResponseAsync();

            Stream dataStream = oResponse.GetResponseStream();
            using (var oReader = new StreamReader(dataStream))
            {
                // Save the actual response
                return await oReader.ReadToEndAsync();
            }
        }

        public async Task QueryUSPS(string sZipCode)
        {
            // TODO
            // Ask USPS about the zip code.
            // USPS (need to be sending mail)
            string sServer = "http://production.shippingapis.com/ShippingAPI.dll";  // Test
            const string sUserID = "771LMGHO5723";
            string sReq = $"{sServer}?API=CityStateLookup&XML=<CityStateLookupRequest%20USERID=\"{sUserID}\"><ZipCode ID=\"0\"><Zip5>{sZipCode}</Zip5></ZipCode></CityStateLookupRequest>";
            m_sResponse = await RequestStringAsync(sReq);
            // TODO populate m_z
            m_z = new ZipCode1 { Zip = sZipCode };
        }

        public async Task QueryWSX_JUNK(string sZipCode)
        {
            // TODO
            // Ask WSX about the zip code.
            // http://webservicex.net/uszip.asmx?op=GetInfoByZIP (out of disk space)
            string sReq = "http://webservicex.net/uszip.asmx/GetInfoByZIP?USZip=" + sZipCode;
            m_sResponse = await RequestStringAsync(sReq);
            // TODO populate m_z
            m_z = new ZipCode1 { Zip = sZipCode };
        }

        public async Task QueryZT(string sZipCode)
        {
            // Ask ZT about the zip code. Working in 2017.
            // http://ziptasticapi.com
            // e.g. sZipCode = "90210";

            string sReq = "http://ziptasticapi.com/" + sZipCode;
            m_sResponse = await RequestStringAsync(sReq);

            // populate m_z from JSON
            // Newtonsoft.Json.JsonConvert.PopulateObject(m_sResponse,m_z);
            var xs = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(ZipCodeZT));
            var x = xs.ReadObject(m_sResponse.ToMemoryStream()) as ZipCodeZT;

            m_z = new ZipCode1
            {
                Zip = sZipCode,
                City = Formatter.ToTitleCase(x.city),   // NOT all caps.
                State = x.state,
                CountryCode = x.country,
            };
            if (m_z.CountryCode == "US")
                m_z.CountryCode = "USA"; // use 3 letter code not 2 letter.
        }

        public async Task<bool> QueryAsync(string sZipCode)
        {
            m_z = null;
            try
            {
                await QueryZT(sZipCode);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
