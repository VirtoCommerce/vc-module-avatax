using Avalara.AvaTax.RestClient;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.TaxModule.Core.Model;

namespace AvaTax.TaxModule.Data.Model
{
    public class AvaLineItem : LineItemModel
    {
        public virtual LineItemModel FromTaxLine(TaxLine taxLine)
        {
            number = taxLine.Id;
            itemCode = taxLine.Code;
            description = taxLine.Name;
            taxCode = taxLine.TaxType;
            amount = taxLine.Amount;
            quantity = taxLine.Quantity;
            return this;
        }

        public virtual LineItemModel FromOrderLine(LineItem orderLine)
        {
            number = orderLine.Id;
            itemCode = orderLine.Sku;
            description = orderLine.Name;
            taxCode = orderLine.TaxType;
            amount = orderLine.ExtendedPrice;
            quantity = orderLine.Quantity;

            return this;
        }

        public virtual LineItemModel FromOrderShipment(Shipment shipment)
        {
            number = shipment.Id;
            itemCode = shipment.Number;
            description = shipment.ShipmentMethodCode;
            quantity = 1;
            amount = shipment.Sum;
            taxCode = shipment.TaxType;

            return this;
        }
    }
}
