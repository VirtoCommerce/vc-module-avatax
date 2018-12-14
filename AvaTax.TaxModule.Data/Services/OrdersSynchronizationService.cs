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
        private const int BatchSize = 50;

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

        public async Task SynchronizeOrdersAsync(IOrdersFeed ordersFeed, Action<AvaTaxOrdersSynchronizationProgress> progressCallback, ICancellationToken cancellationToken)
        {
            var emptyResult = ordersFeed.GetOrders(0, 0);
            var totalCount = emptyResult.TotalCount;

            var progressInfo = new AvaTaxOrdersSynchronizationProgress
            {
                Message = "Reading orders...",
                TotalCount = totalCount,
                ProcessedCount = 0
            };
            progressCallback(progressInfo);

            cancellationToken?.ThrowIfCancellationRequested();

            for (int i = 0; i < totalCount; i += BatchSize)
            {
                var searchResult = ordersFeed.GetOrders(i, BatchSize);
                foreach (var entry in searchResult.Results)
                {
                    var order = entry.CustomerOrder;
                    var store = entry.Store;

                    var avaTaxProvider = store.TaxProviders.FirstOrDefault(x => x.Code == "AvaTaxRateProvider");
                    if (avaTaxProvider != null && avaTaxProvider.IsActive)
                    {
                        var avaTaxSettings = AvaTaxSettings.FromSettings(avaTaxProvider.Settings);
                        var avaTaxClient = _avaTaxClientFactory(avaTaxSettings);

                        var createOrAdjustTransactionModel = AbstractTypeFactory<AvaCreateOrAdjustTransactionModel>.TryCreateInstance();
                        createOrAdjustTransactionModel.FromOrder(order);

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
                    }
                    else
                    {
                        var errorMessage = $"Order #{order.Number} was not sent to Avalara, because the store '{store.Name}' does not use AvaTax as tax provider.";
                        progressInfo.Errors.Add(errorMessage);
                    }

                    cancellationToken?.ThrowIfCancellationRequested();
                }

                var processedCount = Math.Min(i, totalCount);
                progressInfo.ProcessedCount = processedCount;
                progressInfo.Message = $"Processed {processedCount} of {totalCount} orders";
                progressCallback(progressInfo);
            }

            progressInfo.Message = "Orders synchronization completed.";
            progressCallback(progressInfo);
        }
    }
}