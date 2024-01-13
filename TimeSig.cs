using System;
using System.Globalization;
using System.IO;
using System.Net.Sockets;

namespace DotStd
{
    /// <summary>
    /// TODO: A data blob signed for a moment in time. sign the (hash for) blob and the time .
    /// https://www.freetsa.org/index_en.php
    /// Some external service uses a private key to sign the time + some other blob (usually a hash). RFC 3161 compliant with strong 256-bit hash algorithm
    /// We use the public key to determine if this is correct.
    /// https://en.wikipedia.org/wiki/Trusted_timestamping
    /// </summary>
    public class TimeSig // : ExternalService
    {
        // similar to : signtool sign /t "time server url" 
        // Win32 API SignerSignEx, SignerTimeStampEx called via P/Invoke
        // https://github.com/mono/mono/blob/master/mcs/tools/security/signcode.cs
        // signcode.exe -v yourkeypair.pvk -spc yourspc.spc -t http://timestamp.verisign.com/scripts/timstamp.dll yourassembly.exe

        public DateTime Time;   // the time Payload was signed. UTC.
        public byte[]? Sig;     // The signature made with some external private key for Payload + Time
                               // SigType ??   // What algorithms are used for Sig ? What public key ?

        public static bool IsValid(byte[] payload, byte[] publicKey, byte[] sig, DateTime dt)
        {
            // Was/Is this sig+time valid ?
            // payload = the thing that is signed. usually a hash (HashAlgorithm) of some much larger document.
            // TODO
            return false;
        }

        public bool IsValid(byte[] payload, byte[] publicKey)
        {
            // Was/Is this sig+time valid ?
            // Test if Sig and Time is valid for Payload using the public key.
            // TODO
            // Payload = The thing i want to get signed and time stamped. Usually a HASH CODE for the document i want signed.
            return false;
        }

        public void SetSig2( byte[] sig2)
        {
            // unpack a signature from sig+timestamp.
        }

        public void Sign(byte[] payload)
        {
            // Send this out to be signed. The secure external signing service is trusted.
        }

        public void SignLocal(byte[] payload)
        {
            // Create a time stamp from local private key and local time service.
        }
    }
}
