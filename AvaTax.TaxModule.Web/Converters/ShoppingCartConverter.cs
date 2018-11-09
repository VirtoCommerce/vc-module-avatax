using System;
using System.Linq;
using Avalara.AvaTax.RestClient;
using VirtoCommerce.Domain.Cart.Model;
using VirtoCommerce.Domain.Commerce.Model;
using VirtoCommerce.Domain.Customer.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.DynamicProperties;
using AddressType = VirtoCommerce.Domain.Commerce.Model.AddressType;

namespace AvaTax.TaxModule.Web.Converters
{
    public static class ShoppingCartConverter
    {
        [CLSCompliant(false)]
        public static CreateTransactionModel ToAvaTaxCreateTransactionModel(this ShoppingCart cart,
            string companyCode, Member member, bool commit = false)
        {
            if (!cart.Addresses.IsNullOrEmpty() && !cart.Items.IsNullOrEmpty())
            {
                Address shippingAddress = null;
                foreach (var address in cart.Addresses)
                {
                    if (address.AddressType == AddressType.Shipping)
                    {
                        shippingAddress = address;
                        break;
                    }
                }

                var result = new CreateTransactionModel
                {
                    customerCode = cart.CustomerId,
                    date = cart.CreatedDate,
                    companyCode = companyCode,
                    code = cart.Id,
                    commit = commit,
                    type = DocumentType.SalesInvoice,
                    currencyCode = cart.Currency,
                    exemptionNo = member.GetDynamicPropertyValue("Tax exempt", string.Empty),
                    addresses = new AddressesModel()
                    {
                        shipTo = shippingAddress?.ToAvaTaxAddressLocationInfo()
                    }
                };

                result.lines = cart.Items.Select(item => item.ToAvaTaxLineItemModel(shippingAddress)).ToList();
                if (!cart.Shipments.IsNullOrEmpty())
                {
                    result.lines.AddRange(cart.Shipments.Select(shipment => shipment.ToAvaTaxLineItemModel()));
                }

                return result;
            }

            return null;
        }
    }
}