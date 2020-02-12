using System.Diagnostics.Tracing;
using Microsoft.Extensions.Logging;

namespace AvaTax.TaxModule.Data.Logging
{
    public class AvalaraLogger
    {
        private readonly ILogger _logger;

        public AvalaraLogger(ILogger logger)
        {
            _logger = logger;
        }

        #region Context

        public class TaxRequestContext : BaseLogContext
        {
            public string docCode { get; set; }
            public string docType { get; set; }
            public string customerCode { get; set; }
            public double amount { get; set; }
            public bool isCommit { get; set; }
        }

        #endregion

        public class EventCodes
        {
            public const int Startup = 2;
            public const int Ping = 3;
            public const int ValidateAddress = 4;

            public const int ApplicationError = 1001;
            public const int TaxCalculationError = 1000;
            public const int TaxPingError = 1002;
            public const int AddressValidationError = 1003;

            public const int GetTaxRequestTime = 2000;
            public const int GetSalesInvoiceRequestTime = 2001;


        }

        [Event(EventCodes.Startup, Message = "Starting up.", Level = EventLevel.Informational)]
        public void Startup()
        {
            _logger.LogInformation("Starting up."); 
        }

        [Event(EventCodes.Ping, Message = "Test connection passed. Duration {2} ms.", Level = EventLevel.Informational)]
        public void Ping(string startTime, string endTime, double duration)
        {
            _logger.LogInformation($"Test connection passed. Duration {duration} ms.");
        }

        [Event(EventCodes.ValidateAddress, Message = "Address validated successfully. Duration {2} ms.", Level = EventLevel.Informational)]
        public void ValidateAddress(string startTime, string endTime, double duration)
        {
            _logger.LogInformation($"Address validated successfully. Duration {duration} ms.");
        }

        [Event(EventCodes.ApplicationError, Message = "Application Failure: {0}", Level = EventLevel.Critical)]
        public void ApplicationError(string error)
        {
            _logger.LogCritical($"Address validated successfully. Duration {error} ms.");
        }

        [Event(EventCodes.TaxCalculationError, Message = "{0} - {1}. Error message: {2}", Level = EventLevel.Error)]
        public void TaxCalculationError(string docCode, string docType, string error)
        {
            _logger.LogError($"{docCode} - {docType}. Error message: {error}");
        }

        [Event(EventCodes.TaxPingError, Message = "AvaTax ping failed. Error message: {0}", Level = EventLevel.Error)]
        public void TaxPingError(string error)
        {
            _logger.LogError($"AvaTax ping failed. Error message: {error}");
        }

        [Event(EventCodes.AddressValidationError, Message = "Address validation failed. Error message: {0}", Level = EventLevel.Error)]
        public void AddressValidationError(string error)
        {
            _logger.LogError($"Address validation failed. Error message: {error}");
        }

        [Event(EventCodes.GetTaxRequestTime, Message = "{0} - {1}. Duration {4} ms. AvaTax tax request executed successfully.", Level = EventLevel.Informational)]
        public void GetTaxRequestTime(string docCode, string docType, string startTime, string endTime, double duration)
        {
            _logger.LogDebug($"{docCode} - {docType}. Duration {duration} ms. AvaTax tax request executed successfully.");
        }

        [Event(EventCodes.GetSalesInvoiceRequestTime, Message = "{0} - {1}. Commit - {5}. Duration {4} ms. AvaTax tax request executed successfully. Is commit: {5}", Level = EventLevel.Informational)]
        public void GetSalesInvoiceRequestTime(string docCode, string docType, bool isCommit, string startTime, string endTime, double duration)
        {
            _logger.LogDebug($"{docCode} - {docType}. Duration {duration} ms. AvaTax tax request executed successfully. Is commit: {isCommit}");
        }
    }
}