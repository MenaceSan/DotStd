using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace DotStd
{
    public static class SerializeUtil
    {
        // Helper for Generic object serialization.
        // Don't include Newtonsoft JSON here.

        public const string kFalse = "false";   // JSON bool. Not the same as .NET bool values.
        public const string kTrue = "true";     // JSON bool    

        public static string ToJSON(bool b)
        {
            // Get JSON bool string.
            // .NET bool is "True" not "true"
            return b ? kTrue : kFalse;
        }

        public static int ToBase64Len(int lenBin)
        {
            // get base64 string len from binary len.
            return (((lenBin + 2) / 3) * 4); // round up to allow padding it out with ='s
             
        }
        public static int FromBase64Len(int lenBase64)
        {
            // get binary len from base64 len.
            return ((lenBase64 * 3) / 4);
        }

        static Regex _regexBase64 = null;
        public static bool IsValidBase64(string s)
        {
            // Is the format of the string valid for base64?
            // Should not throw on Convert.FromBase64()
            // Convert.ToBase64String()

            if (string.IsNullOrWhiteSpace(s))
                return false;
            s = s.Trim();   // ignore spaces.
            if (_regexBase64 == null)
            {
                _regexBase64 = new Regex(@"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);
            }
            return (s.Length % 4 == 0) && _regexBase64.IsMatch(s);
        }

        public static string ToBase64String(Stream InputStream, int ContentLength)
        {
            // might be from HttpPostedFileBase
            Byte[] b = new byte[ContentLength];
            InputStream.Read(b, 0, b.Length);
            return System.Convert.ToBase64String(b, 0, b.Length);
        }

        public static bool isJSON(string sValue)
        {
            if (string.IsNullOrWhiteSpace(sValue))
                return false;
            if (sValue == "null")   // JSON validly encodes null this way.
                return true;
            return sValue.StartsWith("[") || sValue.StartsWith("{");
        }

        public static bool isXML(string sValue)
        {
            // What about XML file which has UTF-8 BOM marker(EF BB BF) at the beginning ??
            if (string.IsNullOrWhiteSpace(sValue))
                return false;
            return sValue.StartsWith("<");
        }

        public static Dictionary<string, object> ParseJSONToDictionary(string json)
        {
            // Parse JSON to a dictionary. like Newtonsoft?
            // like JSON.NET JsonConvert.DeserializeObject<Dictionary<string, string>>
            // System.Web.Script.Serialization.JavaScriptSerializer is buggy?

            var d = new Dictionary<string, object>();

            if (json.StartsWith("{"))
            {
                json = json.Remove(0, 1);
                if (json.EndsWith("}"))
                    json = json.Substring(0, json.Length - 1);
            }
            json.Trim();

            // Parse out Object Properties from JSON
            while (json.Length > 0)
            {
                var beginProp = json.Substring(0, json.IndexOf(':'));
                json = json.Substring(beginProp.Length);

                var indexOfComma = json.IndexOf(',');
                string endProp;
                if (indexOfComma > -1)
                {
                    endProp = json.Substring(0, indexOfComma);
                    json = json.Substring(endProp.Length);
                }
                else
                {
                    endProp = json;
                    json = string.Empty;
                }

                var curlyIndex = endProp.IndexOf('{');
                if (curlyIndex > -1)
                {
                    var curlyCount = 1;
                    while (endProp.Substring(curlyIndex + 1).IndexOf("{") > -1)
                    {
                        curlyCount++;
                        curlyIndex = endProp.Substring(curlyIndex + 1).IndexOf("{");
                    }
                    while (curlyCount > 0)
                    {
                        endProp += json.Substring(0, json.IndexOf('}') + 1);
                        json = json.Remove(0, json.IndexOf('}') + 1);
                        curlyCount--;
                    }
                }

                json = json.Trim();
                if (json.StartsWith(","))
                    json = json.Remove(0, 1);
                json.Trim();


                // Individual Property (Name/Value Pair) Is Isolated
                var s = (beginProp + endProp).Trim();

                // Now parse the name/value pair out and put into Dictionary
                var name = s.Substring(0, s.IndexOf(":")).Trim();
                var value = s.Substring(name.Length + 1).Trim();

                if (name.StartsWith("\"") && name.EndsWith("\""))
                {
                    name = name.Substring(1, name.Length - 2);
                }

                double valueNumberCheck;
                if (value.StartsWith("\"") && value.StartsWith("\""))
                {
                    // String Value
                    d.Add(name, value.Substring(1, value.Length - 2));
                }
                else if (value.StartsWith("{") && value.EndsWith("}"))
                {
                    // JSON Value
                    d.Add(name, ParseJSONToDictionary(value));
                }
                else if (double.TryParse(value, out valueNumberCheck))
                {
                    // Numeric Value
                    d.Add(name, valueNumberCheck);
                }
                else
                    d.Add(name, value);
            }

            return d;
        }

        public static string SerializeXML(object obj)
        {
            // Create XML string for some object.

            using (var memoryStream = new MemoryStream())
            using (var reader = new StreamReader(memoryStream))
            {
                var serializer = new DataContractSerializer(obj.GetType());
                serializer.WriteObject(memoryStream, obj);
                memoryStream.Position = 0;
                return reader.ReadToEnd();
            }
        }

        public static object DeserializeXML(string xml, Type toType)
        {
            // Create object from XML string.
            using (var stream = new MemoryStream())
            {
                byte[] data = System.Text.Encoding.UTF8.GetBytes(xml);
                stream.Write(data, 0, data.Length);
                stream.Position = 0;
                var deserializer = new DataContractSerializer(toType);
                return deserializer.ReadObject(stream);
            }
        }
    }
}
