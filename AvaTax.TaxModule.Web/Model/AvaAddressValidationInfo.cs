using Avalara.AvaTax.RestClient;
using System;
using VirtoCommerce.Domain.Commerce.Model;

namespace AvaTax.TaxModule.Web.Model
{
    [CLSCompliant(false)]
    public class AvaAddressValidationInfo : AddressValidationInfo
    {
        public virtual AddressValidationInfo FromAddress(Address address)
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