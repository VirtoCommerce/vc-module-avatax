using System;
using Avalara.AvaTax.RestClient;
using VirtoCommerce.Domain.Commerce.Model;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Platform.Core.Common;

namespace AvaTax.TaxModule.Data.Model
{
    [CLSCompliant(false)]
    public class AvaCreateOrAdjustTransactionModel : CreateOrAdjustTransactionModel
    {
        public virtual AvaCreateOrAdjustTransactionModel FromOrder(CustomerOrder order, string companyCode, Address sourceAddress)
        {
            var transaction = AbstractTypeFactory<AvaCreateTransactionModel>.TryCreateInstance();
            transaction.FromOrder(order, companyCode, sourceAddress);
            createTransactionModel = transaction;

            return this;
        }
    }
}