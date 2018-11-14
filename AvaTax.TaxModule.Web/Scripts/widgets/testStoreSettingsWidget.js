angular.module('virtoCommerce.avataxModule')
.controller('virtoCommerce.avataxModule.testStoreSettingsWidgetController', ['$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.avataxModule.resources', function ($scope, bladeNavigationService, avataxModuleResources) {
    $scope.widget.refresh = function () {
        $scope.background = "transparent";
        $scope.message = "";
        bladeNavigationService.setError('', $scope.blade);
    }

    $scope.testConnection = function () {
        $scope.background = "transparent";
        $scope.message = "";
        $scope.blade.isLoading = true;

        var connectionInfo = createAvaTaxConnectionInfo($scope.blade.currentEntity.settings);
        avataxModuleResources.ping(connectionInfo, function () {
            $scope.background = "LightGreen";
            $scope.blade.isLoading = false;
            $scope.message = "Connected successfully!";
            bladeNavigationService.setError('', $scope.blade);
        }, function (error) {
            bladeNavigationService.setError({
                status: 'Error',
                statusText: error.data.message,
                data: error.data
            }, $scope.blade);
            $scope.background = "LightCoral";
            $scope.message = error.data.message;
        });
    }

    function createAvaTaxConnectionInfo(settings) {
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
        return _.find(settings, function(setting) {
            return setting.name === requiredName;
        });
    }

    function getCurrentValueOf(setting) {
        return setting.values[0].value;
    }

    $scope.widget.refresh();
}]);