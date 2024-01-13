using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DotStd
{
    /// <summary>
    /// TODO: A REST provider that does lookups of lat/lon given an address.
    /// related to PostalCodeFinder. PostalCode1
    /// get string city, string state, string zip
    /// </summary>
    public class GeoCodeUtil : ExternalService
    {
        public override string Name => throw new NotImplementedException();
        public override string BaseURL => throw new NotImplementedException();
        public override string Icon => "<i class='fas fa-sync-alt'></i>";

        public GeoLocation? GetLocation(string addr) { return null; }
    }
}
