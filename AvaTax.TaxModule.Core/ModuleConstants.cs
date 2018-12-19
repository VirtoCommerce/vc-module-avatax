namespace AvaTax.TaxModule.Core
{
    public static class ModuleConstants
    {
        public static class Settings
        {
            public const string IsEnabled = "Avalara.Tax.IsEnabled";

            public static class Synchronization
            {
                public const string LastExecutionDate = "Avalara.Tax.Synchronization.LastExecutionDate";
            }

            public static class Credentials
            {
                public const string AccountNumber = "Avalara.Tax.Credentials.AccountNumber";
                public const string LicenseKey = "Avalara.Tax.Credentials.LicenseKey";
                public const string CompanyCode = "Avalara.Tax.Credentials.CompanyCode";
                public const string ServiceUrl = "Avalara.Tax.Credentials.ServiceUrl";
                public const string AdminAreaUrl = "Avalara.Tax.Credentials.AdminAreaUrl";
            }

            public static class ScheduledOrderSynchronization
            {
                public const string IsEnabled = "Avalara.Tax.ScheduledOrdersSynchronization.IsEnabled";
                public const string CronExpression = "Avalara.Tax.ScheduledOrdersSynchronization.CronExpression";
            }
        }

        public static class Permissions
        {
            public const string TaxManage = "tax:manage";
        }

        public const string AvaTaxRateProviderCode = "AvaTaxRateProvider";
    }
}