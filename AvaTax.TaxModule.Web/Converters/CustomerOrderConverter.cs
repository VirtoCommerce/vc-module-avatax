using System;
using System.Collections.Generic;
using System.Linq;
using Avalara.AvaTax.RestClient;
using VirtoCommerce.Domain.Commerce.Model;
using VirtoCommerce.Domain.Customer.Model;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.DynamicProperties;
using AddressType = VirtoCommerce.Domain.Commerce.Model.AddressType;

namespace AvaTax.TaxModule.Web.Converters
{
    public static class CustomerOrderConverter
    {
        [CLSCompliant(false)]
        public static CreateTransactionModel ToAvaTaxCreateTransactionModel(this CustomerOrder order,
            string companyCode, Member member, DocumentType documentType, bool commit = false)
        {
            CreateTransactionModel result = null;

            if (!order.Addresses.IsNullOrEmpty() && !order.Items.IsNullOrEmpty())
            {
                Address shippingAddress = null;
                foreach (var address in order.Addresses)
                {
                    if (address.AddressType == AddressType.Shipping)
                    {
                        shippingAddress = address;
                        break;
                    }
                }

                if (shippingAddress != null)
                {
                    result = new CreateTransactionModel
                    {
                        customerCode = order.CustomerId,
                        date = order.CreatedDate != DateTime.MinValue ? order.CreatedDate : DateTime.UtcNow,
                        companyCode = companyCode,
                        commit = commit,
                        type = documentType,
                        code = order.Number,
                        currencyCode = order.Currency,
                        addresses = new AddressesModel()
                        {
                            // TODO: set actual origin address (fulfillment center)?
                            shipFrom = shippingAddress.ToAvaTaxAddressLocationInfo(),
                            shipTo = shippingAddress.ToAvaTaxAddressLocationInfo()
                        }
                    };

                    result.lines = order.Items.Select(item => item.ToAvaTaxLineItemModel(shippingAddress)).ToList();
                    if (!order.Shipments.IsNullOrEmpty())
                    {
                        result.lines.AddRange(order.Shipments.Select(shipment => shipment.ToAvaTaxLineItemModel()));
                    }
                }
            }

            return result;
        }

        [CLSCompliant(false)]
        public static CreateOrAdjustTransactionModel ToAvaTaxCreateOrAdjustTransactionModel(this CustomerOrder modifiedOrder,
            CustomerOrder originalOrder, string companyCode, Member member, DocumentType documentType, bool commit = false)
        {
            CreateOrAdjustTransactionModel result = null;

            if (!modifiedOrder.Addresses.IsNullOrEmpty() && !originalOrder.Items.IsNullOrEmpty())
            {
                Address shippingAddress = null;
                foreach (var address in modifiedOrder.Addresses)
                {
                    if (address.AddressType == AddressType.Shipping)
                    {
                        shippingAddress = address;
                        break;
                    }
                }

                if (shippingAddress != null)
                {
                    var adjustedTransactionModel = new CreateTransactionModel
                    {
                        customerCode = modifiedOrder.CustomerId,
                        date = DateTime.UtcNow,
                        companyCode = companyCode,
                        code = $"{originalOrder.Number}.{DateTime.UtcNow:yy-MM-dd-hh-mm}",
                        commit = commit,
                        type = documentType,
                        taxOverride = new TaxOverrideModel
                        {
                            type = TaxOverrideType.TaxDate,
                            reason = "Adjustment for return",
                            taxDate = originalOrder.CreatedDate != DateTime.MinValue
                                ? originalOrder.CreatedDate
                                : DateTime.UtcNow,
                            taxAmount = 0.0m
                        },
                        referenceCode = originalOrder.Number,
                        currencyCode = modifiedOrder.Currency,
                        exemptionNo = member.GetDynamicPropertyValue("Tax exempt", string.Empty),
                        addresses = new AddressesModel
                        {
                            // TODO: set actual origin address (fulfillment center)?
                            shipFrom = shippingAddress.ToAvaTaxAddressLocationInfo(),
                            shipTo = shippingAddress.ToAvaTaxAddressLocationInfo()
                        }
                    };

                    var linesToCancel = originalOrder.Items.Where(originalItem =>
                        modifiedOrder.Items.All(x => x.Id != originalItem.Id)
                        || originalItem.Quantity > modifiedOrder.Items.Single(x => x.Id == originalItem.Id).Quantity
                    );

                    var lineItemModels = new List<LineItemModel>();
                    foreach (var originalItem in linesToCancel)
                    {
                        var quantityToCancel = originalItem.Quantity;
                        var amountToCancel = originalItem.Price * originalItem.Quantity;

                        var modifiedItem = modifiedOrder.Items.FirstOrDefault(item => item.Id == originalItem.Id);
                        if (modifiedItem != null)
                        {
                            quantityToCancel -= modifiedItem.Quantity;
                            amountToCancel -= modifiedItem.Price * modifiedItem.Quantity;
                        }

                        lineItemModels.Add(new LineItemModel
                        {
                            number = originalItem.Id,
                            itemCode = originalItem.ProductId,
                            description = originalItem.Name,
                            taxCode = originalItem.TaxType,
                            quantity = quantityToCancel,
                            amount = amountToCancel,
                            addresses = new AddressesModel
                            {
                                // TODO: set actual origin address (fulfillment center)?
                                shipFrom = shippingAddress.ToAvaTaxAddressLocationInfo(),
                                shipTo = shippingAddress.ToAvaTaxAddressLocationInfo()
                            }
                        });
                    }

                    adjustedTransactionModel.lines = lineItemModels;

                    result = new CreateOrAdjustTransactionModel
                    {
                        createTransactionModel = adjustedTransactionModel
                    };
                }
            }

            return result;
        }

        [CLSCompliant(false)]
        public static VoidTransactionModel ToAvaTaxVoidTransactionModel(this CustomerOrder order, VoidReasonCode voidReasonCode)
        {
            if (!order.Addresses.IsNullOrEmpty() && !order.Items.IsNullOrEmpty())
            {
                return new VoidTransactionModel
                {
                    code = voidReasonCode
                };
            }
            return null;
        }
    }
}