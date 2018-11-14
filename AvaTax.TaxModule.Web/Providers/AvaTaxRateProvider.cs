using System;
using System.Collections.Generic;
using System.Linq;
using Avalara.AvaTax.RestClient;
using AvaTax.TaxModule.Data.Model;
using AvaTax.TaxModule.Web.Extensions;
using AvaTax.TaxModule.Web.Logging;
using AvaTax.TaxModule.Web.Services;
using Common.Logging;
using VirtoCommerce.Domain.Common;
using VirtoCommerce.Domain.Tax.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;

namespace AvaTax.TaxModule.Web
{
    public class AvaTaxRateProvider : TaxProvider
    {
        private readonly AvalaraLogger _logger;
        private readonly Func<ITaxSettings, AvaTaxClient> _avaTaxClientFactory;

        public AvaTaxRateProvider()
            : base("AvaTaxRateProvider")
        {
        }

        [CLSCompliant(false)]
        public AvaTaxRateProvider(ILog log, Func<ITaxSettings, AvaTaxClient> avaTaxClientFactory, params SettingEntry[] settings)
            : this()
        {
            Settings = settings;
            _logger = new AvalaraLogger(log);
            _avaTaxClientFactory = avaTaxClientFactory;
        }

        public override IEnumerable<TaxRate> CalculateRates(IEvaluationContext context)
        {
            var taxEvalContext = context as TaxEvaluationContext;
            if (taxEvalContext == null)
            {
                throw new ArgumentException("Given context is not an instance of the TaxEvaluationContext class.", nameof(context));
            }

            var retVal = GetTaxRates(taxEvalContext);
            return retVal;
        }

        protected virtual List<TaxRate> GetTaxRates(TaxEvaluationContext evalContext)
        {
            List<TaxRate> retVal = new List<TaxRate>();
            LogInvoker<AvalaraLogger.TaxRequestContext>.Execute(log =>
            {
                var settings = new SettingsListTaxSettings(Settings);
                Validate(settings);

                //Evaluate taxes only for cart to preventing registration redundant transactions in avalara
                var createTransactionModel = AbstractTypeFactory<AvaCreateTransactionModel>.TryCreateInstance();
                createTransactionModel.FromContext(evalContext);
                createTransactionModel.companyCode = settings.CompanyCode;
                createTransactionModel.commit = false;

                log.docCode = createTransactionModel.code;
                log.docType = createTransactionModel.type.ToString();
                log.customerCode = createTransactionModel.customerCode;
                if (createTransactionModel.IsValid)
                {
                    var avaTaxClient = _avaTaxClientFactory(settings);
                    var transaction = avaTaxClient.CreateTransaction(string.Empty, createTransactionModel);

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

        protected virtual void Validate(ITaxSettings settings)
        {
            if (!settings.IsEnabled || !settings.CredentialsAreFilled())
            {
                throw new Exception("Tax calculation disabled or credentials not provided");
            }
        }
    }
}