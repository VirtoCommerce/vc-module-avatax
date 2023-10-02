using Avalara.AvaTax.RestClient;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.TaxModule.Core.Model;

namespace VirtoCommerce.AvalaraTaxModule.Data.Model
{
    public class AvaValidatedAddressInfo : ValidatedAddressInfo
    {
        public virtual Address ToAddress()
        {
            var address = AbstractTypeFactory<Address>.TryCreateInstance();

            address.Line1 = line1;
            address.Line2 = line2;
            address.City = city;
            address.RegionId = region;
            address.PostalCode = postalCode;
            address.CountryCode = country;
            return address;
        }
    }
}
