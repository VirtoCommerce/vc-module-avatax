using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Settings;

namespace AvaTax.TaxModule.Core
{
    public static class ModuleConstants
    {
        public static class Settings
        {
            public static class Avalara
            {
                public static SettingDescriptor AvalaraTaxEnabled = new SettingDescriptor
                {
                    Name = "Avalara.IsEnabled",
                    GroupName = "Taxes|Avalara",
                    ValueType = SettingValueType.Boolean,
                };

                public static SettingDescriptor AccountNumber = new SettingDescriptor
                {
                    Name = "Avalara.Credentials.AccountNumber",
                    GroupName = "Taxes|Avalara",
                    ValueType = SettingValueType.Integer,
                };

                public static SettingDescriptor LicenseKey = new SettingDescriptor
                {
                    Name = "Avalara.Credentials.LicenseKey",
                    GroupName = "Taxes|Avalara",
                    ValueType = SettingValueType.SecureString,
                };
                public static SettingDescriptor CompanyCode = new SettingDescriptor
                {
                    Name = "Avalara.Credentials.CompanyCode",
                    GroupName = "Taxes|Avalara",
                    ValueType = SettingValueType.ShortText,
                };
                public static SettingDescriptor ServiceUrl = new SettingDescriptor
                {
                    Name = "Avalara.Credentials.ServiceUrl",
                    GroupName = "Taxes|Avalara",
                    ValueType = SettingValueType.ShortText,
                };
                public static SettingDescriptor AdminAreaUrl = new SettingDescriptor
                {
                    Name = "Avalara.Credentials.AdminAreaUrl",
                    GroupName = "Taxes|Avalara",
                    ValueType = SettingValueType.ShortText,
                };
                public static SettingDescriptor SynchronizationCronExpression = new SettingDescriptor
                {
                    Name = "Avalara.ScheduledOrdersSynchronization.CronExpression",
                    GroupName = "Taxes|Avalara",
                    ValueType = SettingValueType.ShortText,
                    DefaultValue = "0 0 * * *"
                };

                public static IEnumerable<SettingDescriptor> AllSettings
                {
                    get
                    {
                        return new List<SettingDescriptor>
                        {
                            AvalaraTaxEnabled,
                            AccountNumber,
                            LicenseKey,
                            CompanyCode,
                            ServiceUrl,
                            AdminAreaUrl,
                            SynchronizationCronExpression
                        };
                    }
                }
            }

            public const string IsEnabled = "Avalara.IsEnabled";

            public static class Synchronization
            {
                public const string LastExecutionDate = "Avalara.Synchronization.LastExecutionDate";
            }

            //public static class Credentials
            //{
            //    public const string AccountNumber = "Avalara.Credentials.AccountNumber";
            //    public const string LicenseKey = "Avalara.Credentials.LicenseKey";
            //    public const string CompanyCode = "Avalara.Credentials.CompanyCode";
            //    public const string ServiceUrl = "Avalara.Credentials.ServiceUrl";
            //    public const string AdminAreaUrl = "Avalara.Credentials.AdminAreaUrl";
            //}

            public static class ScheduledOrderSynchronization
            {
                public const string IsEnabled = "Avalara.ScheduledOrdersSynchronization.IsEnabled";
                public const string CronExpression = "Avalara.ScheduledOrdersSynchronization.CronExpression";
            }
        }


        public static class Security
        {
            public static class Permissions
            {
                public const string TaxManage = "tax:manage";
                public static string[] AllPermissions = new[] { TaxManage };
            }
        }

       // public const string AvaTaxRateProviderCode = "AvaTaxRateProvider";


    }
}
