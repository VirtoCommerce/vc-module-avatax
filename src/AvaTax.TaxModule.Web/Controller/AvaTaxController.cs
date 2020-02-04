using Avalara.AvaTax.RestClient;
using AvaTax.TaxModule.Core;
using AvaTax.TaxModule.Core.Models;
using AvaTax.TaxModule.Core.Services;
using AvaTax.TaxModule.Data.Logging;
using AvaTax.TaxModule.Web.BackgroundJobs;
using AvaTax.TaxModule.Web.Models;
using AvaTax.TaxModule.Web.Models.PushNotifications;
using AvaTax.TaxModule.Web.Services;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.Platform.Core.Security;

namespace AvaTax.TaxModule.Web.Controller
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/tax/avatax")]
    public class AvaTaxController : Microsoft.AspNetCore.Mvc.Controller
    {
        private readonly Func<IAvaTaxSettings, AvaTaxClient> _avaTaxClientFactory;
        private readonly AvalaraLogger _logger;
        private readonly IOrdersSynchronizationService _ordersSynchronizationService;
        private readonly IAddressValidationService _addressValidationService;
        private readonly IPushNotificationManager _pushNotificationManager;
        private readonly IUserNameResolver _userNameResolver;

        [CLSCompliant(false)]
        public AvaTaxController(ILogger<AvaTaxController> log, Func<IAvaTaxSettings, AvaTaxClient> avaTaxClientFactory, IOrdersSynchronizationService ordersSynchronizationService,
            IAddressValidationService addressValidationService, IPushNotificationManager pushNotificationManager, IUserNameResolver userNameResolver)
        {
            _logger = new AvalaraLogger(log);
            _avaTaxClientFactory = avaTaxClientFactory;
            _ordersSynchronizationService = ordersSynchronizationService;
            _addressValidationService = addressValidationService;
            _pushNotificationManager = pushNotificationManager;
            _userNameResolver = userNameResolver;
        }

        [HttpPost]
        [Route("ping")]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        public Task<ActionResult> TestConnection([FromBody]AvaTaxSettings taxSetting)
        {
            ActionResult retVal = BadRequest();
            LogInvoker<AvalaraLogger.TaxRequestContext>.Execute(log =>
            {
                if (taxSetting == null)
                {
                    const string errorMessage = "The connectionInfo parameter is required to test the connection.";
                    retVal = BadRequest(errorMessage);
                    throw new Exception(errorMessage);
                }

                if (!taxSetting.IsEnabled)
                {
                    const string errorMessage = "Tax calculation disabled, enable before testing connection.";
                    retVal = BadRequest(errorMessage);
                    throw new Exception(errorMessage);
                }

                if (!taxSetting.IsValid)
                {
                    const string errorMessage = "AvaTax credentials are not provided.";
                    retVal = BadRequest(errorMessage);
                    throw new Exception(errorMessage);
                }

                var avaTaxClient = _avaTaxClientFactory(taxSetting);

                PingResultModel result;
                try
                {
                    result = avaTaxClient.Ping();
                }
                catch (JsonException e)
                {
                    var errorMessage = $"Avalara API service responded with some unexpected data. Please verify the link to Avalara API service.\nInner error: {e.Message}";
                    retVal = BadRequest(errorMessage);
                    throw new Exception(errorMessage, e);
                }
                catch (Exception e)
                {
                    var errorMessage = $"Failed to connect to the Avalara API due to error: {e.Message}";
                    retVal = BadRequest(errorMessage);
                    throw new Exception(errorMessage, e);
                }

                if (result.authenticated != true)
                {
                    var errorMessage = "Provided Avalara credentials are invalid. Please verify the account number and the license key.";
                    retVal = BadRequest(errorMessage);
                    throw new Exception(errorMessage);
                }

                retVal = Ok(result);
            })
            .OnError(_logger, AvalaraLogger.EventCodes.TaxPingError)
            .OnSuccess(_logger, AvalaraLogger.EventCodes.Ping);

            return Task.FromResult(retVal);
        }

        [HttpPost]
        [Route("orders/synchronize")]
        [Authorize(ModuleConstants.Security.Permissions.TaxManage)]
        public async Task<ActionResult<OrdersSynchronizationPushNotification>> SynchronizeOrders(OrdersSynchronizationRequest request)
        {
            var notification = await Enqueue(request);
            await _pushNotificationManager.SendAsync(notification);
            return Ok(notification);
        }

        [HttpPost]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        [Route("orders/{jobId}/cancel")]
        [Authorize(ModuleConstants.Security.Permissions.TaxManage)]
        public async Task<ActionResult> CancelOrdersSynchronization(string jobId)
        {
            BackgroundJob.Delete(jobId);
            return NoContent();
        }

        [HttpPost]
        [Route("address/validate")]
        public async Task<ActionResult<AddressValidationResult>> ValidateAddress(AddressValidationRequest request)
        {
            var result = await _addressValidationService.ValidateAddressAsync(request.Address, request.StoreId);
            return Ok(result);
        }

        [HttpGet]
        [Route("orders/{orderId}/status")]
        [Authorize(ModuleConstants.Security.Permissions.TaxManage)]
        public async Task<ActionResult<AvaTaxOrderSynchronizationStatus>> GetOrderSynchronizationStatus(string orderId)
        {
            var result = await _ordersSynchronizationService.GetOrderSynchronizationStatusAsync(orderId);
            return Ok(result);
        }

        private async Task<OrdersSynchronizationPushNotification> Enqueue(OrdersSynchronizationRequest request)
        {
            var notification = new OrdersSynchronizationPushNotification(_userNameResolver.GetCurrentUserName())
            {
                Title = "Sending orders to AvaTax",
                Description = "Starting process..."
            };
            await _pushNotificationManager.SendAsync(notification);

            var jobId = BackgroundJob.Enqueue<OrdersSynchronizationJob>(x => x.RunManually(request.OrderIds, notification, JobCancellationToken.Null, null));
            notification.JobId = jobId;

            return notification;
        }
    }
}
