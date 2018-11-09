using System;
using Avalara.AvaTax.RestClient;
using VirtoCommerce.Domain.Cart.Model;
using VirtoCommerce.Domain.Commerce.Model;

namespace AvaTax.TaxModule.Web.Converters
{
    public static class CartShipmentConverter
    {
        [CLSCompliant(false)]
        public static LineItemModel ToAvaTaxLineItemModel(this Shipment shipment)
        {
            return new LineItemModel
            {
                number = shipment.Id ?? shipment.ShipmentMethodCode,
                itemCode = shipment.ShipmentMethodCode,
                quantity = 1,
                amount = shipment.Price,
                description = shipment.ShipmentMethodCode,
                taxCode = shipment.TaxType ?? "FR",
                addresses = new AddressesModel
                {
                    // TODO: set actual origin address (fulfillment center)?
                    shipFrom = shipment.DeliveryAddress.ToAvaTaxAddressLocationInfo(),
                    shipTo = shipment.DeliveryAddress.ToAvaTaxAddressLocationInfo()
                }
            };
        }
    }
}