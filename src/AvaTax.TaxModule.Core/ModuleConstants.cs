using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Settings;

namespace AvaTax.TaxModule.Core
{
    public static class ModuleConstants
    {
        public static class Settings
        {
            public static class Credentials
            {
                public static SettingDescriptor CompanyCode = new SettingDescriptor
                {
                    Name = "Avalara.Credentials.CompanyCode",
                    GroupName = "Tax|Avalara",
                    ValueType = SettingValueType.ShortText,
                };
                public static SettingDescriptor ServiceUrl = new SettingDescriptor
                {
                    Name = "Avalara.Credentials.ServiceUrl",
                    GroupName = "Tax|Avalara",
                    ValueType = SettingValueType.ShortText,
                };
                public static SettingDescriptor AdminAreaUrl = new SettingDescriptor
                {
                    Name = "Avalara.Credentials.AdminAreaUrl",
                    GroupName = "Tax|Avalara",
                    ValueType = SettingValueType.ShortText,
                };

                public static IEnumerable<SettingDescriptor> Settings
                {
                    get
                    {
                        return new List<SettingDescriptor>
                        {
                            CompanyCode,
                            ServiceUrl,
                            AdminAreaUrl,
                        };
                    }
                }

            }

            public static class ScheduledOrdersSynchronization
            {
                public static SettingDescriptor SynchronizationIsEnabled { get; } = new SettingDescriptor
                {
                    Name = "Avalara.ScheduledOrdersSynchronization.IsEnabled",
                    GroupName = "Tax|Avalara",
                    ValueType = SettingValueType.Boolean,
                    DefaultValue = false
                };

                public static SettingDescriptor SynchronizationCronExpression { get; } = new SettingDescriptor
                {
                    Name = "Avalara.ScheduledOrdersSynchronization.CronExpression",
                    GroupName = "Tax|Avalara",
                    ValueType = SettingValueType.ShortText,
                    DefaultValue = "0 0 * * *"
                };

                public static SettingDescriptor LastExecutionDate { get; } = new SettingDescriptor
                {
                    Name = "Avalara.ScheduledOrdersSynchronization.LastExecutionDate",
                    GroupName = "Tax|Avalara",
                    ValueType = SettingValueType.DateTime
                };

                public static IEnumerable<SettingDescriptor> Settings
                {
                    get
                    {
                        return new List<SettingDescriptor>
                        {
                            SynchronizationIsEnabled,
                            SynchronizationCronExpression,
                            LastExecutionDate
                        };
                    }
                }
            }

            public static IEnumerable<SettingDescriptor> AllSettings => Credentials.Settings.Concat(ScheduledOrdersSynchronization.Settings).ToList();
        }

        public static class Security
        {
            public static class Permissions
            {
                public const string TaxManage = "tax:manage";
                public static string[] AllPermissions = new[] { TaxManage };
            }
        }
    }
}
