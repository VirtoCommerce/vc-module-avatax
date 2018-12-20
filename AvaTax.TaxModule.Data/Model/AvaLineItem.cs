using System;
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

        public virtual LineItemModel FromOrderLine(LineItem orderLine, CustomerOrder order, Store store, IFulfillmentCenterService fulfillmentCenterService)
        {
            number = orderLine.Id;
            itemCode = orderLine.Sku;
            description = orderLine.Name;
            taxCode = orderLine.TaxType;
            amount = orderLine.ExtendedPrice;
            quantity = orderLine.Quantity;

            addresses = BuildAddressesModel(orderLine.FulfillmentCenterId, order, store, fulfillmentCenterService);

            return this;
        }

        public virtual LineItemModel FromOrderShipment(Shipment shipment, CustomerOrder order, Store store, IFulfillmentCenterService fulfillmentCenterService)
        {
            number = shipment.Id;
            itemCode = shipment.Number;
            description = shipment.ShipmentMethodCode;
            quantity = 1;
            amount = shipment.Sum;

            // First, let's try to read the tax type from the shipment itself.
            taxCode = shipment.TaxType;

            // If it is not filled, let's find the shipping method for this shipment and take the tax type from there.
            if (string.IsNullOrEmpty(taxCode))
            {
                var shippingMethod = store.ShippingMethods.FirstOrDefault(x => x.Code == shipment.ShipmentMethodCode);
                if (shippingMethod != null)
                {
                    taxCode = shippingMethod.TaxType;
                }
            }

            addresses = BuildAddressesModel(shipment.FulfillmentCenterId, order, store, fulfillmentCenterService);

            return this;
        }

        protected virtual AddressesModel BuildAddressesModel(string itemFulfillmentCenterId, CustomerOrder order, Store store, 
            IFulfillmentCenterService fulfillmentCenterService)
        {
            AddressesModel result = null;

            var shippingAddress = order.Addresses.FirstOrDefault(x => x.AddressType == AddressType.Shipping);
            if (shippingAddress != null)
            {
                var sourceAddress = shippingAddress;

                var fulfillmentCenterId = itemFulfillmentCenterId ?? store.MainFulfillmentCenterId;
                if (!string.IsNullOrEmpty(fulfillmentCenterId))
                {
                    var fulfillmentCenter = fulfillmentCenterService.GetByIds(new[] { fulfillmentCenterId }).FirstOrDefault();
                    if (fulfillmentCenter != null)
                    {
                        sourceAddress = fulfillmentCenter.Address;
                    }
                }

                var avaTaxSourceAddress = AbstractTypeFactory<AvaAddressLocationInfo>.TryCreateInstance();
                avaTaxSourceAddress.FromAddress(sourceAddress);

                var avaTaxDestinationAddress = AbstractTypeFactory<AvaAddressLocationInfo>.TryCreateInstance();
                avaTaxDestinationAddress.FromAddress(shippingAddress);

                result = new AddressesModel
                {
                    shipFrom = avaTaxSourceAddress,
                    shipTo = avaTaxDestinationAddress
                };
            }

            return result;
        }
    }
}