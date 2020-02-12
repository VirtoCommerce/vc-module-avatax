using AvaTax.TaxModule.Core.Models;
using System.Threading.Tasks;
using VirtoCommerce.TaxModule.Core.Model;

namespace AvaTax.TaxModule.Core.Services
{
    public interface IAddressValidationService
    {
        Task<AddressValidationResult> ValidateAddressAsync(Address address, string storeId);
    }
}