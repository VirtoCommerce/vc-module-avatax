using Avalara.AvaTax.RestClient;
using VirtoCommerce.TaxModule.Core.Model;

namespace AvaTax.TaxModule.Data.Model
{
    public class AvaAddressValidationInfo : AddressValidationInfo
    {
        public virtual AvaAddressValidationInfo FromAddress(Address address)
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
