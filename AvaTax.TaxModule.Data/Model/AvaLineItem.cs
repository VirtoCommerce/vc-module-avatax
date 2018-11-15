using System;
using Avalara.AvaTax.RestClient;
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
    }
}