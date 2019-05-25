using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace DotStd
{
    [Serializable]
    public class PostalCode1
    {
        // A Postal code decodes to this. for use with GeoLocation.
        // Poco - don't collide with the EF version of this. Use "PostalCode1" name.
        // https://gis.stackexchange.com/questions/53918/determining-which-us-zipcodes-map-to-more-than-one-state-or-more-than-one-city

        public const int kMaxLen = 32;   // there are no countries that use PostalCode > 32 (i'm pretty sure)

        public string PostalCode { get; set; }      // AKA Zip Code/ZipCode can have multiple possible cities (Not PK)!

        public string City { get; set; }            // Converted to TitleCase. Maybe was all caps e.g. "ALLSTON". 
        public string State { get; set; }           // 2 letter code "MA" (can be added to cache dynamically) (should exist in geo_state db)
        public string CountryCode { get; set; }     // 3 letter code. "USA" (MUST exist in geo_country db)


        public static string CityStatePostal(string city, string state, string postal, string country = null)
        {
            // Format "city, state postal" and account for missing info in USA normal style. 
            // uses Formatter.ToTitleCase 

            string ret = Formatter.JoinTitles(", ", city, state);
            if (ValidState.IsValidUnique(postal))
            {
                if (ret.Length > 0)
                    ret += " ";
                ret += postal;
            }
            if (!string.IsNullOrWhiteSpace(country))
            {
                if (ret.Length > 0)
                    ret += " ";
                ret += '(' + country + ')';
            }
            return ret;
        }

        public static CountryId GetCountryFromPostal(string code, CountryId eCountryDef = CountryId.ANY)
        {
            // Is this a valid PostalCode for a country ? USA, Canada

            // http://stackoverflow.com/questions/578406/what-is-the-ultimate-postal-code-and-zip-regex

            // "US"=>"^\d{5}([\-]?\d{4})?$",
            // "UK"=>"^(GIR|[A-Z]\d[A-Z\d]??|[A-Z]{2}\d[A-Z\d]??)[ ]??(\d[A-Z]{2})$",
            // "DE"=>"\b((?:0[1-46-9]\d{3})|(?:[1-357-9]\d{4})|(?:[4][0-24-9]\d{3})|(?:[6][013-9]\d{3}))\b",
            // "CA"=>"^([ABCEGHJKLMNPRSTVXY]\d[ABCEGHJKLMNPRSTVWXYZ])\ {0,1}(\d[ABCEGHJKLMNPRSTVWXYZ]\d)$",
            // "FR"=>"^(F-)?((2[A|B])|[0-9]{2})[0-9]{3}$",
            // "IT"=>"^(V-|I-)?[0-9]{5}$",
            // "AU"=>"^(0[289][0-9]{2})|([1345689][0-9]{3})|(2[0-8][0-9]{2})|(290[0-9])|(291[0-4])|(7[0-4][0-9]{2})|(7[8-9][0-9]{2})$",
            // "NL"=>"^[1-9][0-9]{3}\s?([a-zA-Z]{2})?$",
            // "ES"=>"^([1-9]{2}|[0-9][1-9]|[1-9][0-9])[0-9]{3}$",
            // "DK"=>"^([D-d][K-k])?( |-)?[1-9]{1}[0-9]{3}$",
            // "SE"=>"^(s-|S-){0,1}[0-9]{3}\s?[0-9]{2}$",
            // "BE"=>"^[1-9]{1}[0-9]{3}$"

            if (Regex.IsMatch(code, @"^\d{5}(-\d{4})?$"))
                return CountryId.USA;

            if (Regex.IsMatch(code, @"^(s-|S-){0,1}[0-9]{3}\s?[0-9]{2}$")) // pass="12345|||932 68|||S-621 46", fail="5367|||425611|||31 545"
                return CountryId.SWE;

            // Canada
            // Lower case letters are not strictly allowed. but we allow them.
            if (Regex.IsMatch(code, @"^[ABCEGHJKLMNPRSTVXYabceghjklmnprstvxy]{1}\d{1}[A-Za-z]{1} *\d{1}[A-Za-z]{1}\d{1}$"))
                return CountryId.CAN;

            // Australian postal code verification. 
            // Australia has 4-digit numeric postal codes with the following state based specific ranges. ACT: 0200-0299 and 2600-2639. NSW: 1000-1999, 2000-2599 and 2640-2914. NT: 0900-0999 and 0800-0899. QLD: 9000-9999 and 4000-4999. SA: 5000-5999. TAS: 7800-7999 and 7000-7499. VIC: 8000-8999 and 3000-3999. WA: 6800-6999 and 6000-6799
            if (Regex.IsMatch(code, @"^(0[289][0-9]{2})|([1345689][0-9]{3})|(2[0-8][0-9]{2})|(290[0-9])|(291[0-4])|(7[0-4][0-9]{2})|(7[8-9][0-9]{2})$"))
                return CountryId.AUS;

            // Look for bad chars or bad length that no valid zip would use?
            return CountryId.ANY;   // no idea what country this might be.
        }

        public static GeoStateId GetStateFromPostalUS(int zip)
        {
            // Convert US Postal codes to GeoStateId. 
            // https://en.wikipedia.org/wiki/ZIP_Code

            if ((zip >= 600 && zip <= 799) || (zip >= 900 && zip <= 999)) // Puerto Rico (00600-00799 and 900--00999 ranges)
                return GeoStateId.PR;
            else if (zip >= 800 && zip <= 899) // US Virgin Islands (00800-00899 range)            
                return GeoStateId.VI;
            else if (zip >= 1000 && zip <= 2799) // Massachusetts (01000-02799 range)
                return GeoStateId.MA;
            else if (zip >= 2800 && zip <= 2999) // Rhode Island (02800-02999 range)
                return GeoStateId.RI;
            else if (zip >= 3000 && zip <= 3899) // New Hampshire (03000-03899 range)
                return GeoStateId.NH;
            else if (zip >= 3900 && zip <= 4999) // Maine (03900-04999 range)
                return GeoStateId.ME;
            else if (zip >= 5000 && zip <= 5999) // Vermont (05000-05999 range)
                return GeoStateId.VT;
            else if ((zip >= 6000 && zip <= 6999) && zip != 6390) // Connecticut (06000-06999 range excluding 6390)
                return GeoStateId.CT;
            else if (zip >= 70000 && zip <= 8999) // New Jersey (07000-08999 range)
                return GeoStateId.NJ;
            else if ((zip >= 10000 && zip <= 14999) || zip == 6390 || zip == 501 || zip == 544) // New York (10000-14999 range and 6390, 501, 544)
                return GeoStateId.NY;
            else if (zip >= 15000 && zip <= 19699) // Pennsylvania (15000-19699 range)
                return GeoStateId.PA;
            else if (zip >= 19700 && zip <= 19999) // Delaware (19700-19999 range)
                return GeoStateId.DE;
            else if ((zip >= 20000 && zip <= 20099) || (zip >= 20200 && zip <= 20599) || (zip >= 56900 && zip <= 56999)) // District of Columbia (20000-20099, 20200-20599, and 56900-56999 ranges)
                return GeoStateId.DC;
            else if (zip >= 20600 && zip <= 21999) // Maryland (20600-21999 range)            
                return GeoStateId.MD;
            else if ((zip >= 20100 && zip <= 20199) || (zip >= 22000 && zip <= 24699)) // Virginia (20100-20199 and 22000-24699 ranges, also some taken from 20000-20099 DC range)
                return GeoStateId.VA;
            else if (zip >= 24700 && zip <= 26999) // West Virginia (24700-26999 range)
                return GeoStateId.WV;
            else if (zip >= 27000 && zip <= 28999) // North Carolina (27000-28999 range)
                return GeoStateId.NC;
            else if (zip >= 29000 && zip <= 29999) // South Carolina (29000-29999 range)            
                return GeoStateId.SC;
            else if ((zip >= 30000 && zip <= 31999) || (zip >= 39800 && zip <= 39999)) // Georgia (30000-31999, 39901[Atlanta] range)
                return GeoStateId.GA;
            else if (zip >= 32000 && zip <= 34999) // Florida (32000-34999 range)
                return GeoStateId.FL;
            else if (zip >= 35000 && zip <= 36999) // Alabama (35000-36999 range)
                return GeoStateId.AL;
            else if (zip >= 37000 && zip <= 38599) // Tennessee (37000-38599 range)
                return GeoStateId.TN;
            else if (zip >= 38600 && zip <= 39799) // Mississippi (38600-39999 range)
                return GeoStateId.MS;
            else if (zip >= 40000 && zip <= 42799) // Kentucky (40000-42799 range)
                return GeoStateId.KY;
            else if (zip >= 43000 && zip <= 45999) // Ohio (43000-45999 range)
                return GeoStateId.OH;
            else if (zip >= 46000 && zip <= 47999) // Indiana (46000-47999 range)
                return GeoStateId.IN;
            else if (zip >= 48000 && zip <= 49999) // Michigan (48000-49999 range)
                return GeoStateId.MI;
            else if (zip >= 50000 && zip <= 52999) // Iowa (50000-52999 range)
                return GeoStateId.IA;
            else if (zip >= 53000 && zip <= 54999) // Wisconsin (53000-54999 range)
                return GeoStateId.WI;
            else if (zip >= 55000 && zip <= 56799) // Minnesota (55000-56799 range)
                return GeoStateId.MN;
            else if (zip >= 57000 && zip <= 57999) // South Dakota (57000-57999 range)
                return GeoStateId.SD;
            else if (zip >= 58000 && zip <= 58999) // North Dakota (58000-58999 range)
                return GeoStateId.ND;
            else if (zip >= 59000 && zip <= 59999) // Montana (59000-59999 range)
                return GeoStateId.MT;
            else if (zip >= 60000 && zip <= 62999) // Illinois (60000-62999 range)
                return GeoStateId.IL;
            else if (zip >= 63000 && zip <= 65999) // Missouri (63000-65999 range)
                return GeoStateId.MO;
            else if (zip >= 66000 && zip <= 67999) // Kansas (66000-67999 range)
                return GeoStateId.KS;
            else if (zip >= 68000 && zip <= 69999) // Nebraska (68000-69999 range)
                return GeoStateId.NE;
            else if (zip >= 70000 && zip <= 71599) // Louisiana (70000-71599 range)
                return GeoStateId.LA;
            else if (zip >= 71600 && zip <= 72999) // Arkansas (71600-72999 range)
                return GeoStateId.AR;
            else if (zip >= 73000 && zip <= 74999) // Oklahoma (73000-74999 range)
                return GeoStateId.OK;
            else if ((zip >= 75000 && zip <= 79999) || (zip >= 88500 && zip <= 88599)) // Texas (75000-79999 and 88500-88599 ranges)
                return GeoStateId.TX;
            else if (zip >= 80000 && zip <= 81999) // Colorado (80000-81999 range)
                return GeoStateId.CO;
            else if (zip >= 82000 && zip <= 83199) // Wyoming (82000-83199 range)
                return GeoStateId.WY;
            else if (zip >= 83200 && zip <= 83999) // Idaho (83200-83999 range)
                return GeoStateId.ID;
            else if (zip >= 84000 && zip <= 84999) // Utah (84000-84999 range)
                return GeoStateId.UT;
            else if (zip >= 85000 && zip <= 86999) // Arizona (85000-86999 range)
                return GeoStateId.AZ;
            else if (zip >= 87000 && zip <= 88499) // New Mexico (87000-88499 range)
                return GeoStateId.NM;
            else if (zip >= 88900 && zip <= 89999) // Nevada (88900-89999 range)
                return GeoStateId.NV;
            else if (zip >= 90000 && zip <= 96199) // California (90000-96199 range)
                return GeoStateId.CA;
            else if (zip >= 96700 && zip <= 96899) // Hawaii (96700-96899 range)  
                return GeoStateId.HI;
            else if (zip >= 97000 && zip <= 97999) // Oregon (97000-97999 range)
                return GeoStateId.OR;
            else if (zip >= 98000 && zip <= 99499) // Washington (98000-99499 range)
                return GeoStateId.WA;
            else if (zip >= 99500 && zip <= 99999) // Alaska (99500-99999 range)
                return GeoStateId.AK;

            return GeoStateId.UNK;
        }

        public static GeoStateId GetStateFromPostal(string postal)
        {
            string[] temp = postal.Split('-');

            if (GetCountryFromPostal(temp[0]) == CountryId.USA)
                return GetStateFromPostalUS(Converter.ToInt(temp[0]));

            return GeoStateId.UNK;
        }
    }

    [DataContract]
    public class PostalCodeZT
    {
        // Poco for ZT REST service
        [DataMember] public string country { get; set; } // 2 letter code! US
        [DataMember] public string state { get; set; }   // MA
        [DataMember] public string city { get; set; }    // ALSTON
    }

    public class PostalCodeFinder
    {
        //! Call Web Service to look up a Postal code. Maybe call IsValidPostalCode() first ?
        //! if a Postal code is not in my local cache i can look it up here.
        //! compliments GeoLocation
        // https://stackoverflow.com/questions/7129313/zip-code-lookup-api
        // https://www.melissa.com/lookups/ZipCityPhone.asp?InData=02134

        public string m_sResponse;  // Raw response.
        public PostalCode1 m_z;        // parsed m_sResponse.

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

        public async Task QueryUSPS(string sPostalCode)
        {
            // TODO
            // Ask USPS about the Postal code.
            // USPS (need to be sending mail)
            string sServer = "http://production.shippingapis.com/ShippingAPI.dll";  // Test
            const string sUserID = "771LMGHO5723";
            string sReq = $"{sServer}?API=CityStateLookup&XML=<CityStateLookupRequest%20USERID=\"{sUserID}\"><ZipCode ID=\"0\"><Zip5>{sPostalCode}</Zip5></ZipCode></CityStateLookupRequest>";
            m_sResponse = await RequestStringAsync(sReq);
            // TODO populate m_z
            m_z = new PostalCode1 { PostalCode = sPostalCode };
        }

        public async Task QueryWSX_JUNK(string sPostalCode)
        {
            // TODO
            // Ask WSX about the Postal code.
            // http://webservicex.net/uszip.asmx?op=GetInfoByZIP (out of disk space)
            string sReq = "http://webservicex.net/uszip.asmx/GetInfoByZIP?USZip=" + sPostalCode;
            m_sResponse = await RequestStringAsync(sReq);
            // TODO populate m_z
            m_z = new PostalCode1 { PostalCode = sPostalCode };
        }

        public async Task QueryZT(string sPostalCode)
        {
            // Ask ZT about the Postal code. Working in 2017.
            // http://ziptasticapi.com
            // e.g. sPostalCode = "90210";

            string sReq = "http://ziptasticapi.com/" + sPostalCode;
            m_sResponse = await RequestStringAsync(sReq);

            // populate m_z from JSON
            // Newtonsoft.Json.JsonConvert.PopulateObject(m_sResponse,m_z);
            var xs = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(PostalCodeZT));
            var x = xs.ReadObject(m_sResponse.ToMemoryStream()) as PostalCodeZT;

            m_z = new PostalCode1
            {
                PostalCode = sPostalCode,
                City = Formatter.ToTitleCase(x.city),   // NOT all caps.
                State = x.state,
                CountryCode = x.country,
            };
            if (m_z.CountryCode == "US")
                m_z.CountryCode = "USA"; // use 3 letter code not 2 letter.
        }

        public async Task<bool> QueryAsync(string sPostalCode)
        {
            m_z = null;
            try
            {
                await QueryZT(sPostalCode);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
