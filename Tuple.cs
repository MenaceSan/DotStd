using System;

namespace DotStd
{
    /// <summary>
    /// Tuple<int, string?> but serializable.
    /// like KeyValuePair<int, string> (but I cant init KeyValuePair properly so its not useful for JSON or EF)
    /// like class System.Tuple<int,string> NOT like struct System.ValueTuple<int,string>
    /// </summary>
    [Serializable]  // JSON
    public class TupleIdValue // : Tuple<int, string?>
    {
        public int Id { get; set; } // int PK unique id.
        public string Value { get; set; } // null/empty is NOT ok

        public TupleIdValue()
        {
            // De-Serialize construct.
            Value = string.Empty;   // Not really valid.
        }
        public TupleIdValue(int id, string val)
        {
            Id = id; Value = val;
        }
        public TupleIdValue(Enum e)
        {
            Id = e.ToInt(); Value = e.ToDescription();
        }
    }

    /// <summary>
    /// like KeyValuePair<string, string> (but cant init KeyValuePair properly and not useful for JSON or EF)
    /// like class System.Tuple<string,string>
    /// </summary>
    [Serializable]  // JSON
    public class TupleKeyValue // : Tuple<string, string>
    {
        public string Key { get; set; }  // MUST be a unique string.
        public string Value { get; set; }

        public TupleKeyValue()
        {
            // De-Serialize construct.
            Key = string.Empty;  // Not really valid.
            Value = string.Empty;   // Not really valid.
        }
        public TupleKeyValue(string key, string nameValue)
        {
            Key = key; Value = nameValue;
        }
        public TupleKeyValue(Enum e)
        {
            Key = e.ToString(); Value = e.ToDescription();
        }
    }
}
