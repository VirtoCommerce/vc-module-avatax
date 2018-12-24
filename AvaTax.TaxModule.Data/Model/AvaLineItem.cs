﻿using System;
using System.Linq;
using Avalara.AvaTax.RestClient;
using VirtoCommerce.Domain.Commerce.Model;
using VirtoCommerce.Domain.Inventory.Services;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Domain.Tax.Model;
using VirtoCommerce.Platform.Core.Common;

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
            itemCode = shipment.Number;
            description = shipment.ShipmentMethodCode;
            quantity = 1;
            amount = shipment.Sum;
            taxCode = shipment.TaxType;

            return this;
        }
    }
}