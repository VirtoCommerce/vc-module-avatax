using System;
using Avalara.AvaTax.RestClient;
using VirtoCommerceAddress = VirtoCommerce.Domain.Commerce.Model.Address;

namespace AvaTax.TaxModule.Web.Converters
{
    public static class CustomerAddressConverter
    {
        [CLSCompliant(false)]
        public static AddressLocationInfo ToAvaTaxAddressLocationInfo(this VirtoCommerceAddress address)
        {
            return new AddressLocationInfo
            {
                line1 = address.Line1,
                line2 = address.Line2,
                city = address.City,
                region = address.RegionName,
                postalCode = address.PostalCode,
                country = address.CountryName
            };
        }

        [CLSCompliant(false)]
        public static AddressValidationInfo ToAddressValidationInfo(this VirtoCommerceAddress address)
        {
            return new AddressValidationInfo
            {
                line1 = address.Line1,
                line2 = address.Line2,
                city = address.City,
                region = address.RegionName,
                postalCode = address.PostalCode,
                country = address.CountryName
            };
        }
    }
}