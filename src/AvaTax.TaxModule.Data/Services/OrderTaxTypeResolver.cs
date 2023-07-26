using System;
using System.Linq;
using System.Threading.Tasks;
using AvaTax.TaxModule.Core.Services;
using VirtoCommerce.CoreModule.Core.Tax;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ShippingModule.Core.Model.Search;
using VirtoCommerce.ShippingModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Services;

namespace AvaTax.TaxModule.Data.Services
{
    public class OrderTaxTypeResolver : IOrderTaxTypeResolver
    {
        private readonly IStoreService _storeService;
        private readonly IShippingMethodsSearchService _shippingMethodsSearchService;

        public OrderTaxTypeResolver(IStoreService storeService, IShippingMethodsSearchService shippingMethodsSearchService)
        {
            _storeService = storeService;
            _shippingMethodsSearchService = shippingMethodsSearchService;
        }

        public async Task ResolveTaxTypeForOrderAsync(CustomerOrder order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }
            var store = await _storeService.GetByIdAsync(order.StoreId);
            if (store != null)
            {
                var shipmentMethodCode = order.Shipments?.FirstOrDefault()?.ShipmentMethodCode;

                var shippingMethods = await _shippingMethodsSearchService.SearchAsync(new ShippingMethodsSearchCriteria
                {
                    IsActive = true,
                    Keyword = shipmentMethodCode
                });

                //TODO: Takes the default Tax Type from first active shipping method relevant to first shipment of order (this behavior absolutely doesn't generic and can't be used for order has the multiple shipments)
                var defaultTaxType = shippingMethods.Results.FirstOrDefault()?.TaxType;
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
