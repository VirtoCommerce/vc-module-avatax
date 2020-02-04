using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalara.AvaTax.RestClient;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.TaxModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Model;
using Address = VirtoCommerce.CoreModule.Core.Common.Address;

namespace AvaTax.TaxModule.Data.Model
{
    [CLSCompliant(false)]
    public class AvaCreateTransactionModel : CreateTransactionModel
    {
        public virtual bool IsValid => addresses != null && !lines.IsNullOrEmpty();

        public virtual AvaCreateTransactionModel FromContext(TaxEvaluationContext context, string requiredCompanyCode)
        {
            code = context.Id;
            // TODO: customerCode is required by AvaTax API, but using stub values when the customer is not specified doesn't seem right...
            customerCode = context.Customer?.Id ?? Thread.CurrentPrincipal?.Identity?.Name ?? "undef";
            companyCode = requiredCompanyCode;
            date = DateTime.UtcNow;
            type = DocumentType.SalesOrder;
            currencyCode = context.Currency;
            if (context.Lines != null)
            {
                lines = new List<LineItemModel>();
                foreach (var taxLine in context.Lines.Where(x => !x.IsTransient()))
                {
                    var avaLine = AbstractTypeFactory<AvaLineItem>.TryCreateInstance();
                    avaLine.FromTaxLine(taxLine);
                    lines.Add(avaLine);
                }
            }
            if (context.Address != null)
            {
                var avaAddress = AbstractTypeFactory<AvaAddressLocationInfo>.TryCreateInstance();
                avaAddress.FromAddress(context.Address);
                addresses = new AddressesModel
                {
                    // TODO: set actual origin address (fulfillment center/store owner)?
                    shipFrom = avaAddress,
                    shipTo = avaAddress
                };
            }

            return this;
        }

        public virtual AvaCreateTransactionModel FromOrder(CustomerOrder order, string requiredCompanyCode, Address sourceAddress)
        {
            code = order.Number;
            customerCode = order.CustomerId;
            date = order.CreatedDate;
            type = DocumentType.SalesInvoice;
            currencyCode = order.Currency;
            companyCode = requiredCompanyCode;

            var shippingAddress = order.Addresses.FirstOrDefault(x => x.AddressType == AddressType.Shipping);
            if (shippingAddress != null && sourceAddress != null)
            {
                var avaSourceAddress = AbstractTypeFactory<AvaAddressLocationInfo>.TryCreateInstance();
                avaSourceAddress.FromAddress(sourceAddress);

                var avaShippingAddress = AbstractTypeFactory<AvaAddressLocationInfo>.TryCreateInstance();
                avaShippingAddress.FromAddress(shippingAddress);

                addresses = new AddressesModel
                {
                    shipFrom = avaSourceAddress,
                    shipTo = avaShippingAddress
                };
            }

            lines = new List<LineItemModel>();
            foreach (var orderLine in order.Items.Where(x => !x.IsTransient()))
            {
                var avaTaxLine = AbstractTypeFactory<AvaLineItem>.TryCreateInstance();
                avaTaxLine.FromOrderLine(orderLine);
                lines.Add(avaTaxLine);
            }

            foreach (var shipment in order.Shipments ?? Enumerable.Empty<Shipment>())
            {
                var avaTaxLine = AbstractTypeFactory<AvaLineItem>.TryCreateInstance();
                avaTaxLine.FromOrderShipment(shipment);
                lines.Add(avaTaxLine);
            }

            return this;
        }
    }
}