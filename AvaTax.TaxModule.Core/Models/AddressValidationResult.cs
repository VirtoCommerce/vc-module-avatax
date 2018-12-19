using VirtoCommerce.Domain.Commerce.Model;

namespace AvaTax.TaxModule.Core.Models
{
    public class AddressValidationResult
    {
        public Address Address { get; set; }

        public bool AddressIsValid { get; set; }

        public string[] Messages { get; set; }
    }
}