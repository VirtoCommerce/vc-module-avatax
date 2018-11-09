using System;
using System.Linq;
using Avalara.AvaTax.RestClient;
using VirtoCommerce.Domain.Tax.Model;
using VirtoCommerce.Platform.Core.Common;

namespace AvaTax.TaxModule.Web.Converters
{
    public static class TaxRequestConverter
    {
        [CLSCompliant(false)]
        public static CreateTransactionModel ToAvaTaxCreateTransactionModel(this TaxEvaluationContext evalContext,
            string companyCode, bool commit = false)
        {
            if (evalContext.Address != null && !evalContext.Lines.IsNullOrEmpty())
            {
                var result = new CreateTransactionModel()
                {
                    code = evalContext.Id,
                    // TODO: customerCode is required by AvaTax API, but using stub values when the customer is not specified doesn't seem right...
                    customerCode = evalContext.Customer?.Id ?? "(not specified)",
                    date = DateTime.UtcNow,
                    companyCode = companyCode,
                    type = DocumentType.SalesOrder,
                    commit = commit,
                    currencyCode = evalContext.Currency,
                    addresses = new AddressesModel
                    {
                        // TODO: set actual origin address (fulfillment center/store owner)?
                        shipFrom = evalContext.Address.ToAvaTaxAddressLocationInfo(),
                        shipTo = evalContext.Address.ToAvaTaxAddressLocationInfo()
                    },
                    lines = evalContext.Lines.Select(line => new LineItemModel
                    {
                        number = line.Id,
                        itemCode = line.Code,
                        description = line.Name,
                        taxCode = line.TaxType,
                        amount = line.Amount,
                        quantity = line.Quantity,
                    }).ToList()
                };

                // TODO: fill some more info?
                return result;
            }

            return null;
        }
    }
}