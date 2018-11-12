﻿using Avalara.AvaTax.RestClient;
using System;
using System.Collections.Generic;
using System.Threading;
using VirtoCommerce.Domain.Tax.Model;
using VirtoCommerce.Platform.Core.Common;

namespace AvaTax.TaxModule.Web.Model
{
    [CLSCompliant(false)]
    public class AvaCreateTransactionModel : CreateTransactionModel
    {
        public virtual AvaCreateTransactionModel FromContext(TaxEvaluationContext context)
        {
            code = context.Id;
            // TODO: customerCode is required by AvaTax API, but using stub values when the customer is not specified doesn't seem right...
            customerCode = context.Customer?.Id ?? Thread.CurrentPrincipal?.Identity?.Name ?? "undef";
            date = DateTime.UtcNow;
            companyCode = companyCode;
            type = DocumentType.SalesOrder;
            commit = commit;
            currencyCode = context.Currency;
            if (context.Lines != null)
            {
                lines = new List<LineItemModel>();
                foreach (var taxLine in context.Lines)
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
    }
}