using Avalara.AvaTax.RestClient;
using AvaTax.TaxModule.Web.Logging;
using AvaTax.TaxModule.Web.Model;
using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Domain.Common;
using VirtoCommerce.Domain.Tax.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;

namespace AvaTax.TaxModule.Web
{
    public class AvaTaxRateProvider : TaxProvider
    {
        #region const

        private const string AccountNumberPropertyName = "Avalara.Tax.Credentials.AccountNumber";
        private const string LicenseKeyPropertyName = "Avalara.Tax.Credentials.LicenseKey";
        private const string ServiceUrlPropertyName = "Avalara.Tax.Credentials.ServiceUrl";
        private const string CompanyCodePropertyName = "Avalara.Tax.Credentials.CompanyCode";
        private const string IsEnabledPropertyName = "Avalara.Tax.IsEnabled";

        #endregion

        private readonly AvalaraLogger _logger;
        private readonly Func<AvaTaxClient> _avaTaxClientFactory;

        public AvaTaxRateProvider()
            : base("AvaTaxRateProvider")
        {
        }

        [CLSCompliant(false)]
        public AvaTaxRateProvider(ILog log, Func<AvaTaxClient> avaTaxClientFactory, params SettingEntry[] settings)
            : this()
        {
            Settings = settings;
            _logger = new AvalaraLogger(log);
            _avaTaxClientFactory = avaTaxClientFactory;
        }

        private int AccountNumber => int.Parse(GetSetting(AccountNumberPropertyName));

        private string LicenseKey => GetSetting(LicenseKeyPropertyName);

        private string CompanyCode => GetSetting(CompanyCodePropertyName);

        private string ServiceUrl => GetSetting(ServiceUrlPropertyName);

        private bool IsEnabled => bool.Parse(GetSetting(IsEnabledPropertyName));

        public override IEnumerable<TaxRate> CalculateRates(IEvaluationContext context)
        {
            var taxEvalContext = context as TaxEvaluationContext;
            if (taxEvalContext == null)
            {
                throw new NullReferenceException("taxEvalContext");
            }

            var retVal = GetTaxRates(taxEvalContext);
            return retVal;
        }

        protected virtual List<TaxRate> GetTaxRates(TaxEvaluationContext evalContext)
        {
            List<TaxRate> retVal = new List<TaxRate>();
            LogInvoker<AvalaraLogger.TaxRequestContext>.Execute(log =>
            {
                Validate();

                //Evaluate taxes only for cart to preventing registration redundant transactions in avalara
                var createTransactionModel = AbstractTypeFactory<AvaCreateTransactionModel>.TryCreateInstance();
                createTransactionModel.FromContext(evalContext);
                createTransactionModel.companyCode = CompanyCode;
                createTransactionModel.commit = false;

                log.docCode = createTransactionModel.code;
                log.docType = createTransactionModel.type.ToString();
                log.customerCode = createTransactionModel.customerCode;
                if (createTransactionModel.IsValid)
                {
                    var avaTaxClient = _avaTaxClientFactory();
                    var transaction = avaTaxClient.CreateTransaction(string.Empty, createTransactionModel);
                    // TODO: error handling?

                    if (!transaction.lines.IsNullOrEmpty())
                    {
                        foreach (var taxLine in transaction.lines)
                        {
                            var rate = new TaxRate
                            {
                                Rate = taxLine.tax ?? 0.0m,
                                Currency = evalContext.Currency,
                                TaxProvider = this,
                                Line = evalContext.Lines.FirstOrDefault(x => x.Id == taxLine.lineNumber)
                            };
                            if (rate.Line != null)
                            {
                                retVal.Add(rate);
                            }
                        }
                    }
                }

            })
            .OnError(_logger, AvalaraLogger.EventCodes.TaxCalculationError)
            .OnSuccess(_logger, AvalaraLogger.EventCodes.GetSalesInvoiceRequestTime);

            return retVal;
        }

        protected virtual void Validate()
        {
            if (!IsEnabled || AccountNumber == 0
                    || string.IsNullOrEmpty(LicenseKey)
                    || string.IsNullOrEmpty(ServiceUrl)
                    || string.IsNullOrEmpty(CompanyCode))
            {
                throw new Exception("Tax calculation disabled or credentials not provided");
            }
        }
    }
}