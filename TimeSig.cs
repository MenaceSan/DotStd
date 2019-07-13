using System;

namespace DotStd
{
    public class TimeSig1
    {
        // A data blob signed for a moment in time. sign the blob and the time .
        // https://www.freetsa.org/index_en.php
        // Some external service uses a private key to sign the time + some other blob (usually a hash).
        // We use the public key to determine if this is correct.

        public byte[] Payload;       // The thing i want to get signed and time stamped.
    }

    public class TimeSig2 : TimeSig1
    {
        public DateTime Time;   // the time Payload was signed. UTC.
        public byte[] Sig;     // The signature made with some external private key for Payload
        // SigType ??   // What algorithms are used for Sig ? What public key ?

        public bool IsValid(byte[] publicKey)
        {
            // Test if Sig and Time is valid for Payload.
            // Was this valid ?
            // TODO
            return false;
        }
    }
}
