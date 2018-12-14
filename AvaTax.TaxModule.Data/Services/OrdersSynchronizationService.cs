using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Avalara.AvaTax.RestClient;
using AvaTax.TaxModule.Core.Models;
using AvaTax.TaxModule.Core.Services;
using AvaTax.TaxModule.Data.Model;
using AvaTax.TaxModule.Web.Services;
using VirtoCommerce.Domain.Order.Services;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.Platform.Core.Common;

namespace AvaTax.TaxModule.Data.Services
{
    public class OrdersSynchronizationService : IOrdersSynchronizationService
    {
        private readonly ICustomerOrderService _orderService;
        private readonly IStoreService _storeService;
        private readonly Func<IAvaTaxSettings, AvaTaxClient> _avaTaxClientFactory;

        public OrdersSynchronizationService(ICustomerOrderService orderService, IStoreService storeService, Func<IAvaTaxSettings, AvaTaxClient> avaTaxClientFactory)
        {
            _orderService = orderService;
            _storeService = storeService;
            _avaTaxClientFactory = avaTaxClientFactory;
        }

        public async Task<AvaTaxOrderSynchronizationStatus> GetOrderSynchronizationStatusAsync(string orderId)
        {
            var order = _orderService.GetByIds(new[] {orderId}).FirstOrDefault();
            if (order == null)
            {
                throw new ArgumentException("Order with given ID does not exist.", nameof(orderId));
            }

            var result = AbstractTypeFactory<AvaTaxOrderSynchronizationStatus>.TryCreateInstance();

            var store = _storeService.GetById(order.StoreId);
            var avaTaxProvider = store.TaxProviders.FirstOrDefault(x => x.Code == "AvaTaxRateProvider");
            if (avaTaxProvider != null && avaTaxProvider.IsActive)
            {
                result.StoreUsesAvaTax = true;

                var avaTaxSettings = AvaTaxSettings.FromSettings(avaTaxProvider.Settings);
                var avaTaxClient = _avaTaxClientFactory(avaTaxSettings);

                try
                {
                    var transactionModel = await avaTaxClient.GetTransactionByCodeAsync(store.Id, order.Number, 
                        DocumentType.SalesInvoice, string.Empty);
                    result.LastSynchronizationDate = transactionModel.modifiedDate;
                }
                catch (AvaTaxError e) when (e.statusCode == HttpStatusCode.NotFound)
                {
                    // The transaction does not exist in Avalara - the order had probably never been sent there.
                    // This is normal, and we shouldn't treat it as error.
                    result.LastSynchronizationDate = null;
                }
                catch (AvaTaxError e)
                {
                    var errorDetails = e.error.error;
                    var joinedMessages = string.Join(Environment.NewLine,
                        errorDetails.details.Select(x => $"{x.severity}: {x.message} {x.description}"));

                    var errorMessage = $"{errorDetails.message}{Environment.NewLine}{joinedMessages}";
                    result.Errors = new[] { errorMessage };
                    result.HasErrors = true;
                }
            }
            else
            {
                result.StoreUsesAvaTax = false;
            }

            return result;
        }

        public async Task SynchronizeOrdersAsync(string[] orderIds, Action<AvaTaxOrdersSynchronizationProgress> progressCallback, ICancellationToken cancellationToken)
        {
            var progressInfo = new AvaTaxOrdersSynchronizationProgress
            {
                Message = "Reading orders...",
                TotalCount = orderIds.Length,
                ProcessedCount = 0
            };
            progressCallback(progressInfo);

            var orders = _orderService.GetByIds(orderIds).GroupBy(order => order.StoreId);

            var storeIds = orders.Select(orderGroup => orderGroup.Key).ToArray();
            var stores = _storeService.GetByIds(storeIds).ToDictionary(store => store.Id, store => store);

            cancellationToken?.ThrowIfCancellationRequested();

            foreach (var orderGroup in orders)
            {
                var store = stores[orderGroup.Key];

                progressInfo.Message = $"Processing orders from store '{store.Name}'...";
                progressCallback(progressInfo);

                var avaTaxProvider = store.TaxProviders.FirstOrDefault(x => x.Code == "AvaTaxRateProvider");
                if (avaTaxProvider != null && avaTaxProvider.IsActive)
                {
                    var avaTaxSettings = AvaTaxSettings.FromSettings(avaTaxProvider.Settings);
                    var avaTaxClient = _avaTaxClientFactory(avaTaxSettings);

                    foreach (var order in orderGroup)
                    {
                        var createOrAdjustTransactionModel = AbstractTypeFactory<AvaCreateOrAdjustTransactionModel>.TryCreateInstance();
                        createOrAdjustTransactionModel.FromOrder(order);

                        progressInfo.ProcessedCount++;
                        progressInfo.Message = $"Processed {progressInfo.ProcessedCount} of {progressInfo.TotalCount} orders";

                        try
                        {
                            var transactionModel = await avaTaxClient.CreateOrAdjustTransactionAsync(string.Empty, createOrAdjustTransactionModel);
                        }
                        catch (AvaTaxError e)
                        {
                            var errorDetails = e.error.error;
                            var joinedMessages = string.Join(Environment.NewLine,
                                errorDetails.details.Select(x => $"{x.severity}: {x.message} {x.description}"));

                            var errorMessage = $"Order #{order.Number}: {errorDetails.message}{Environment.NewLine}{joinedMessages}";
                            progressInfo.Errors.Add(errorMessage);
                        }
                        finally
                        {
                            progressCallback(progressInfo);
                        }
                    }
                }
                else
                {
                    var errorMessage = $"Orders from store '{store.Name}' were not sent to Avalara, because this store does not use AvaTax as tax provider.";
                    progressInfo.Errors.Add(errorMessage);

                    progressInfo.ProcessedCount += orderGroup.Count();
                    progressCallback(progressInfo);
                }

                cancellationToken?.ThrowIfCancellationRequested();
            }

            progressInfo.Message = "Orders synchronization completed.";
            progressCallback(progressInfo);
        }
    }
}