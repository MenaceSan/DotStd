using System;

namespace DotStd
{
    [Serializable]  // JSON
    public struct TupleIdValue // : ValueTuple<int, string>
    {
        // like KeyValuePair<int, string> (but cant init KeyValuePair properly so its not useful for JSON)
        // like struct System.ValueTuple<int,string>

        public int Id { get; set; }
        public string Value { get; set; }
 
        public TupleIdValue(int id, string val)
        {
            Id = id; Value = val;
        }
        public TupleIdValue(Enum e)
        {
            Id = e.ToInt(); Value = e.ToDescription();
        }
    }

    [Serializable]  // JSON
    public class TupleKeyValue // : Tuple<string, string>
    {
        // like KeyValuePair<string, string> (but cant init KeyValuePair properly and not useful for JSON)
        // like class System.Tuple<string,string>

        public string Key { get; set; } // a unique string.
        public string Value { get; set; }
 
        public TupleKeyValue()
        {
        }
        public TupleKeyValue(string key, string name)
        {
            Key = key; Value = name;
        }
    } 
}
