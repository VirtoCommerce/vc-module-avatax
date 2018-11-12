angular.module('virtoCommerce.avataxModule')
.controller('virtoCommerce.avataxModule.avataxWidgetController', ['$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.avataxModule.resources', function ($scope, bladeNavigationService, avataxModuleResources) {
    $scope.widget.refresh = function () {
        $scope.background = "transparent";
        $scope.message = "";
        bladeNavigationService.setError('', $scope.blade);
    }

    $scope.testConnection = function () {
        $scope.background = "transparent";
        $scope.message = "";
        $scope.blade.isLoading = true;
        avataxModuleResources.ping(function () {
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

    $scope.widget.refresh();
}]);