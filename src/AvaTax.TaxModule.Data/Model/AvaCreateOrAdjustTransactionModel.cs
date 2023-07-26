using Avalara.AvaTax.RestClient;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using Address = VirtoCommerce.CoreModule.Core.Common.Address;

namespace AvaTax.TaxModule.Data.Model
{
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
