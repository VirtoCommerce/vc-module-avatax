//Call this to register our module to main application
var moduleName = "virtoCommerce.avataxModule";

if (AppDependencies != undefined) {
    AppDependencies.push(moduleName);
}

angular.module(moduleName, [])
    .factory('virtoCommerce.avataxModule.avaSettingsFactory', [function () {

        function loadAvaSettings(settings) {
            var isEnabledSetting = findSettingByName(settings, "Avalara.Tax.IsEnabled");
            var accountNumberSetting = findSettingByName(settings, "Avalara.Tax.Credentials.AccountNumber");
            var licenseKeySetting = findSettingByName(settings, "Avalara.Tax.Credentials.LicenseKey");
            var serviceUrlSetting = findSettingByName(settings, "Avalara.Tax.Credentials.ServiceUrl");
            var companyCodeSetting = findSettingByName(settings, "Avalara.Tax.Credentials.CompanyCode");

            var result = null;
            if (isEnabledSetting && accountNumberSetting && licenseKeySetting && serviceUrlSetting && companyCodeSetting) {
                result = {
                    isEnabled: getCurrentValueOf(isEnabledSetting),
                    accountNumber: getCurrentValueOf(accountNumberSetting),
                    licenseKey: getCurrentValueOf(licenseKeySetting),
                    serviceUrl: getCurrentValueOf(serviceUrlSetting),
                    companyCode: getCurrentValueOf(companyCodeSetting)
                };
            }

            return result;
        }

        function findSettingByName(settings, requiredName) {
            return _.find(settings, function (setting) {
                return setting.name === requiredName;
            });
        }

        function getCurrentValueOf(setting) {
            return setting.values[0].value;
        }
        return {
            loadAvaSettings: loadAvaSettings,
        };
    }])
    .run(
        ['platformWebApp.toolbarService', 'platformWebApp.dialogService', 'virtoCommerce.avataxModule.resources', 'platformWebApp.mainMenuService', 'platformWebApp.widgetService', 'platformWebApp.authService', function (toolbarService, dialogService, avataxModuleResources, mainMenuService, widgetService, authService) {

            // Register widget on Avalara tax provider properties blade
            widgetService.registerWidget({
                isVisible: function (blade) {
                    return blade.currentEntity.name == 'Avalara taxes' && authService.checkPermission('tax:manage');
                },
                controller: 'virtoCommerce.avataxModule.testAvataxConnectionWidgetController',
                template: 'Modules/$(Avalara.Tax)/Scripts/widgets/testAvataxConnectionWidget.tpl.html'
            },
                'taxProviderDetail');

            // Register widget in module settings
            widgetService.registerWidget({
                isVisible: function (blade) {
                    return blade.currentEntities && blade.currentEntities['Avalara'];
                },
                controller: 'virtoCommerce.avataxModule.testAvataxConnectionWidgetController',
                template: 'Modules/$(Avalara.Tax)/Scripts/widgets/testAvataxConnectionWidget.tpl.html'
            },
                'settingsDetail');
        }])
    ;