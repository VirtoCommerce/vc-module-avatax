using VirtoCommerce.Domain.Order.Model;

namespace AvaTax.TaxModule.Core.Services
{
    /// <summary>
    /// Represents an abstraction to fill the tax type for all order object graph
    /// </summary>
    public interface IOrderTaxTypeResolver
    {
        void ResolveTaxTypeForOrder(CustomerOrder order);
    }
}