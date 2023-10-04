using Avalara.AvaTax.RestClient;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.TaxModule.Core.Model;

namespace VirtoCommerce.AvalaraTaxModule.Data.Model
{
    public static class ValidatedAddressInfoExtension
    {
        public static Address ToAddress(this ValidatedAddressInfo info)
        {
            var address = AbstractTypeFactory<Address>.TryCreateInstance();

            address.Line1 = info.line1;
            address.Line2 = info.line2;
            address.City = info.city;
            address.RegionId = info.region;
            address.PostalCode = info.postalCode;
            address.CountryCode = info.country;

            return address;
        }
    }
}
