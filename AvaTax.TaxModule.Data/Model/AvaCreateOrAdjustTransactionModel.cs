using Avalara.AvaTax.RestClient;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Platform.Core.Common;

namespace AvaTax.TaxModule.Data.Model
{
    public class AvaCreateOrAdjustTransactionModel : CreateOrAdjustTransactionModel
    {
        public virtual AvaCreateOrAdjustTransactionModel FromOrder(CustomerOrder order)
        {
            var transaction = AbstractTypeFactory<AvaCreateTransactionModel>.TryCreateInstance();
            transaction.FromOrder(order);
            createTransactionModel = transaction;

            return this;
        }
    }
}