using Avalara.AvaTax.RestClient;
using VirtoCommerce.Platform.Core.Common;
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
            region = address.RegionId;
            postalCode = address.PostalCode;
            country = address.CountryCode;
            return this;
        }
    }
}
