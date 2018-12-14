using System;
using Avalara.AvaTax.RestClient;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Domain.Tax.Model;

namespace AvaTax.TaxModule.Data.Model
{
    [CLSCompliant(false)]
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
            itemCode = shipment.ShipmentMethodCode;
            description = shipment.ShipmentMethodCode;
            taxCode = shipment.TaxType;
            quantity = 1;
            amount = shipment.Sum;
            return this;
        }
    }
}