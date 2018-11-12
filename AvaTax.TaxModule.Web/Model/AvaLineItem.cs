using Avalara.AvaTax.RestClient;
using System;
using VirtoCommerce.Domain.Tax.Model;

namespace AvaTax.TaxModule.Web.Model
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
    }
}