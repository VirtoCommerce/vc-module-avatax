using System.Linq;
using AvaTax.TaxModule.Core.Services;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Domain.Store.Services;

namespace AvaTax.TaxModule.Data.Services
{
    public class TaxTypeAdjustmentService : ITaxTypeAdjustmentService
    {
        private readonly IStoreService _storeService;

        public TaxTypeAdjustmentService(IStoreService storeService)
        {
            _storeService = storeService;
        }

        public void AdjustTaxTypesFor(CustomerOrder order)
        {
            foreach (var shipment in order.Shipments)
            {
                if (string.IsNullOrEmpty(shipment.TaxType))
                {
                    var store = _storeService.GetByIds(new[] {order.StoreId}).SingleOrDefault();
                    if (store != null)
                    {
                        var shippingMethod = store.ShippingMethods.FirstOrDefault(x => x.Code == shipment.ShipmentMethodCode);
                        shipment.TaxType = shippingMethod?.TaxType;
                    }
                }
            }
        }
    }
}