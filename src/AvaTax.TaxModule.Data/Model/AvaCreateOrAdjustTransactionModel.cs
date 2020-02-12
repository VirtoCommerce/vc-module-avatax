using Avalara.AvaTax.RestClient;
using System;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using Address = VirtoCommerce.CoreModule.Core.Common.Address;

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