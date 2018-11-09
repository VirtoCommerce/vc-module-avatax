using Avalara.AvaTax.RestClient;
using VirtoCommerce.Domain.Cart.Model;
using VirtoCommerce.Domain.Commerce.Model;

namespace AvaTax.TaxModule.Web.Converters
{
    public static class CartLineItemConverter
    {
        public static LineItemModel ToAvaTaxLineItemModel(this LineItem lineItem, Address shippingAddress)
        {
            return new LineItemModel
            {
                number = lineItem.ProductId,
                itemCode = lineItem.Sku,
                quantity = lineItem.Quantity,
                amount = lineItem.ExtendedPrice,
                description = lineItem.Name,
                taxCode = lineItem.TaxType,
                addresses = new AddressesModel
                {
                    // TODO: set actual origin address (fulfillment center?)
                    shipTo = shippingAddress.ToAvaTaxAddressLocationInfo()
                }
            };
        }
    }
}