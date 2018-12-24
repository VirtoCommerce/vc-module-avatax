using System;
using System.Linq;
using AvaTax.TaxModule.Core.Services;
using VirtoCommerce.Domain.Commerce.Model;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.Platform.Core.Common;

namespace AvaTax.TaxModule.Data.Services
{
    public class OrderTaxTypeResolver : IOrderTaxTypeResolver
    {
        private readonly IStoreService _storeService;

        public OrderTaxTypeResolver(IStoreService storeService)
        {
            _storeService = storeService;
        }

        public void ResolveTaxTypeForOrder(CustomerOrder order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }
            var store = _storeService.GetByIds(new[] { order.StoreId }).SingleOrDefault();
            if (store != null)
            {
                //Take the default TaxType from first active shipment method for the store of passed order (this behavior absolutely doesn't generic and should be changed in the next releases)
                var defaultTaxType = store.ShippingMethods.FirstOrDefault(x => x.IsActive)?.TaxType;
                if (defaultTaxType != null)
                {
                    var taxableObjects = order.GetFlatObjectsListWithInterface<ITaxable>();
                    foreach (var taxableObj in taxableObjects)
                    {
                        taxableObj.TaxType = string.IsNullOrEmpty(taxableObj.TaxType) ? defaultTaxType : taxableObj.TaxType;
                    }
                }
            }
        }
    }
}