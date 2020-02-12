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
using Microsoft.Extensions.Options;

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
        private readonly AvaTaxSecureOptions _options;

        [CLSCompliant(false)]
        public AvaTaxController(ILogger<AvaTaxController> log, Func<IAvaTaxSettings, AvaTaxClient> avaTaxClientFactory, IOrdersSynchronizationService ordersSynchronizationService,
            IAddressValidationService addressValidationService, IPushNotificationManager pushNotificationManager, IUserNameResolver userNameResolver, IOptions<AvaTaxSecureOptions> options)
        {
            _logger = new AvalaraLogger(log);
            _avaTaxClientFactory = avaTaxClientFactory;
            _ordersSynchronizationService = ordersSynchronizationService;
            _addressValidationService = addressValidationService;
            _pushNotificationManager = pushNotificationManager;
            _userNameResolver = userNameResolver;
            _options = options.Value;
        }

        [HttpPost]
        [Route("ping")]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(PingResultModel), StatusCodes.Status200OK)]
        public Task<ActionResult> TestConnection([FromBody]AvaTaxSettings taxSetting)
        {
            ActionResult result = BadRequest();
            LogInvoker<AvalaraLogger.TaxRequestContext>.Execute(log =>
            {
                if (taxSetting == null)
                {
                    const string errorMessage = "The connectionInfo parameter is required to test the connection.";
                    result = BadRequest(errorMessage);
                    throw new Exception(errorMessage);
                }

                taxSetting.AccountNumber = _options.AccountNumber;
                taxSetting.LicenseKey = _options.LicenseKey;

                if (!taxSetting.IsValid)
                {
                    const string errorMessage = "AvaTax credentials are not provided.";
                    result = BadRequest(errorMessage);
                    throw new Exception(errorMessage);
                }

                var avaTaxClient = _avaTaxClientFactory(taxSetting);

                PingResultModel pingResult;
                try
                {
                    pingResult = avaTaxClient.Ping();
                }
                catch (JsonException e)
                {
                    var errorMessage = $"Avalara API service responded with some unexpected data. Please verify the link to Avalara API service.\nInner error: {e.Message}";
                    result = BadRequest(errorMessage);
                    throw new Exception(errorMessage, e);
                }
                catch (Exception e)
                {
                    var errorMessage = $"Failed to connect to the Avalara API due to error: {e.Message}";
                    result = BadRequest(errorMessage);
                    throw new Exception(errorMessage, e);
                }

                if (pingResult.authenticated != true)
                {
                    var errorMessage = "Provided Avalara credentials are invalid. Please verify the account number and the license key.";
                    result = BadRequest(errorMessage);
                    throw new Exception(errorMessage);
                }

                result = Ok(pingResult);
            })
            .OnError(_logger, AvalaraLogger.EventCodes.TaxPingError)
            .OnSuccess(_logger, AvalaraLogger.EventCodes.Ping);

            return Task.FromResult(result);
        }

        [HttpPost]
        [Route("orders/synchronize")]
        [Authorize(ModuleConstants.Security.Permissions.TaxManage)]
        public async Task<ActionResult<OrdersSynchronizationPushNotification>> SynchronizeOrders([FromBody]OrdersSynchronizationRequest request)
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
        public async Task<ActionResult<AddressValidationResult>> ValidateAddress([FromBody] AddressValidationRequest request)
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
