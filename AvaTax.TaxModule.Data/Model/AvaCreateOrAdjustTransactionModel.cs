using Avalara.AvaTax.RestClient;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Platform.Core.Common;

namespace AvaTax.TaxModule.Data.Model
{
    public class AvaCreateOrAdjustTransactionModel : CreateOrAdjustTransactionModel
    {
        public virtual AvaCreateOrAdjustTransactionModel FromOrder(CustomerOrder order, string companyCode)
        {
            var transaction = AbstractTypeFactory<AvaCreateTransactionModel>.TryCreateInstance();
            transaction.FromOrder(order, companyCode);
            createTransactionModel = transaction;

            return this;
        }
    }
}