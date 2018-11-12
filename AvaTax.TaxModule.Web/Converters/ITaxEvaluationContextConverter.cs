using System;
using Avalara.AvaTax.RestClient;
using VirtoCommerce.Domain.Tax.Model;

namespace AvaTax.TaxModule.Web.Converters
{
    public interface ITaxEvaluationContextConverter
    {
        [CLSCompliant(false)]
        CreateTransactionModel ConvertToCreateTransactionModel(TaxEvaluationContext context, string companyCode, bool commit);
    }
}