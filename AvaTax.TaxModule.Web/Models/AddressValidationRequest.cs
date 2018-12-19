using VirtoCommerce.Domain.Commerce.Model;

namespace AvaTax.TaxModule.Web.Models
{
    public class AddressValidationRequest
    {
        /// <summary>
        /// ID of the store from which the validation request had been made.
        /// Needed for obtaining store-specific Avalara connection settings.
        /// </summary>
        public string StoreId { get; set; }

        /// <summary>
        /// Address to validate.
        /// </summary>
        public Address Address { get; set; }
    }
}