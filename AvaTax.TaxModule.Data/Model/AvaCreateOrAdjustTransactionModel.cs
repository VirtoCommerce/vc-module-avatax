using Avalara.AvaTax.RestClient;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Platform.Core.Common;

namespace AvaTax.TaxModule.Data.Model
{
    public class AvaCreateOrAdjustTransactionModel : CreateOrAdjustTransactionModel
    {
        public virtual AvaCreateOrAdjustTransactionModel FromOrder(CustomerOrder order, Store store, string companyCode)
        {
            var transaction = AbstractTypeFactory<AvaCreateTransactionModel>.TryCreateInstance();
            transaction.FromOrder(order, store, companyCode);
            createTransactionModel = transaction;

            return this;
        }
    }
}