using System;
using System.Collections.Generic;
using System.Linq;
using Avalara.AvaTax.RestClient;
using VirtoCommerce.Domain.Tax.Model;
using VirtoCommerce.Platform.Core.Common;

namespace AvaTax.TaxModule.Web.Converters
{
    [CLSCompliant(false)]
    public class TaxEvaluationContextConverterImpl : ITaxEvaluationContextConverter
    {
        public virtual CreateTransactionModel ConvertToCreateTransactionModel(TaxEvaluationContext context, string companyCode, bool commit)
        {
            if (context.Address != null && !context.Lines.IsNullOrEmpty())
            {
                var result = new CreateTransactionModel()
                {
                    code = context.Id,
                    // TODO: customerCode is required by AvaTax API, but using stub values when the customer is not specified doesn't seem right...
                    customerCode = context.Customer?.Id ?? "(not specified)",
                    date = DateTime.UtcNow,
                    companyCode = companyCode,
                    type = DocumentType.SalesOrder,
                    commit = commit,
                    currencyCode = context.Currency,
                    addresses = GetAddressesModelFor(context),
                    lines = GetLineItemModelsFor(context).ToList()
                };

                // TODO: fill some more info?
                return result;
            }

            return null;
        }

        protected virtual AddressesModel GetAddressesModelFor(TaxEvaluationContext context)
        {
            return new AddressesModel
            {
                // TODO: set actual origin address (fulfillment center/store owner)?
                shipFrom = context.Address.ToAvaTaxAddressLocationInfo(),
                shipTo = context.Address.ToAvaTaxAddressLocationInfo()
            };
        }

        protected virtual IEnumerable<LineItemModel> GetLineItemModelsFor(TaxEvaluationContext context)
        {
            return context.Lines.Select(GetLineItemModel);
        }

        protected virtual LineItemModel GetLineItemModel(TaxLine taxLine)
        {
            return new LineItemModel
            {
                number = taxLine.Id,
                itemCode = taxLine.Code,
                description = taxLine.Name,
                taxCode = taxLine.TaxType,
                amount = taxLine.Amount,
                quantity = taxLine.Quantity,
            };
        }
    }
}