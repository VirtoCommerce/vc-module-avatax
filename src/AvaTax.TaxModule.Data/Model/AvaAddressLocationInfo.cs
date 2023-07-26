using Avalara.AvaTax.RestClient;
using VirtoCommerce.CoreModule.Core.Common;

namespace AvaTax.TaxModule.Data.Model
{
    public class AvaAddressLocationInfo : AddressLocationInfo
    {
        public virtual AddressLocationInfo FromAddress(Address address)
        {
            line1 = address.Line1;
            line2 = address.Line2;
            city = address.City;
            region = address.RegionName;
            postalCode = address.PostalCode;
            country = address.CountryName;
            return this;
        }
    }
}
