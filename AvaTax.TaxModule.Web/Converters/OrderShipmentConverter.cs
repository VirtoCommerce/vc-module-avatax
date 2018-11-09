using System;
using Avalara.AvaTax.RestClient;
using VirtoCommerce.Domain.Order.Model;

namespace AvaTax.TaxModule.Web.Converters
{
    public static class OrderShipmentConverter
    {
        [CLSCompliant(false)]
        public static LineItemModel ToAvaTaxLineItemModel(this Shipment shipment)
        {
            return new LineItemModel
            {
                number = shipment.Id ?? shipment.ShipmentMethodCode,
                itemCode = shipment.ShipmentMethodCode,
                quantity = 1,
                amount = shipment.Sum,
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