using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Order.Events;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;

namespace AvaTax.TaxModule.Web.Handlers
{
    public class OrderTaxAdjustmentHandler : IEventHandler<OrderChangeEvent>
    {
        private readonly IStoreService _storeService;

        public OrderTaxAdjustmentHandler(IStoreService storeService)
        {
            _storeService = storeService;
        }

        public Task Handle(OrderChangeEvent message)
        {
            foreach (var entry in message.ChangedEntries)
            {
                if (entry.EntryState == EntryState.Added || entry.EntryState == EntryState.Modified)
                {
                    var order = entry.NewEntry;
                    var store = _storeService.GetById(order.StoreId);
                    var taxProvider = store.TaxProviders.FirstOrDefault(x => x.Code == typeof(AvaTaxRateProvider).Name);
                    if (taxProvider != null && taxProvider.IsActive && taxProvider is AvaTaxRateProvider avaTaxRateProvider)
                    {
                        if (entry.EntryState == EntryState.Added)
                        {
                            avaTaxRateProvider.CalculateOrderTax(order);
                        }
                        else if (entry.EntryState == EntryState.Modified)
                        {
                            avaTaxRateProvider.AdjustOrderTax(entry.OldEntry, entry.NewEntry);
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}