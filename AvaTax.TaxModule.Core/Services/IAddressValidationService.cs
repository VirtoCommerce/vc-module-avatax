using AvaTax.TaxModule.Core.Models;
using VirtoCommerce.Domain.Commerce.Model;

namespace AvaTax.TaxModule.Core.Services
{
    public interface IAddressValidationService
    {
        AddressValidationResult ValidateAddress(Address address, string storeId);
    }
}