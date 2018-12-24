using VirtoCommerce.Domain.Order.Model;

namespace AvaTax.TaxModule.Core.Services
{
    public interface ITaxTypeAdjustmentService
    {
        void AdjustTaxTypesFor(CustomerOrder order);
    }
}