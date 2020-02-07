angular.module('virtoCommerce.avataxModule')
    .controller('virtoCommerce.avataxModule.testAvataxConnectionWidgetController', ['$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.avataxModule.resources', 'virtoCommerce.avataxModule.avaSettingsFactory', function ($scope, bladeNavigationService, avataxModuleResources, avaSettingsFactory) {
    $scope.widget.refresh = function () {
        $scope.background = "transparent";
        $scope.message = "";
        bladeNavigationService.setError('', $scope.blade);
    }

    $scope.testConnection = function () {
        $scope.background = "transparent";
        $scope.message = "";
        $scope.blade.isLoading = true;
        var settings = undefined;
        //Try to get settings if widget is showed in platform settings blade
        if ($scope.blade.currentEntities) {
            settings = $scope.blade.currentEntities['Avalara'];
        }
        if (!settings) {
            settings = $scope.blade.currentEntity.settings;
        }
        if (settings) {
            var avaSettings = avaSettingsFactory.loadAvaSettings(settings);
            avataxModuleResources.ping(avaSettings, function () {
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
    }    
    $scope.widget.refresh();
}]);
