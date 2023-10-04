using System.Collections.Generic;
using VirtoCommerce.TaxModule.Core.Model;

namespace AvaTax.TaxModule.Core.Models
{
    public class AddressValidationResult
    {
        public Address Address { get; set; }

        public List<Address> ValidatedAddresses { get; set; }

        public bool AddressIsValid { get; set; }

        public string[] Messages { get; set; }
    }
}
