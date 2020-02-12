using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.OrdersModule.Core.Model;

namespace AvaTax.TaxModule.Core.Services
{
    /// <summary>
    /// Represents an abstraction to fill the tax type for all order object graph
    /// </summary>
    public interface IOrderTaxTypeResolver
    {
        Task ResolveTaxTypeForOrderAsync(CustomerOrder order);
    }
}
