angular.module('virtoCommerce.avataxModule')
.controller('virtoCommerce.avataxModule.runOrderSynchronizationWidgetController', ['$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.avataxModule.resources', function ($scope, bladeNavigationService, avataxModuleResources) {
    $scope.widget.refresh = function () {
        bladeNavigationService.setError('', $scope.blade);
    }

    $scope.runOrderSynchronization = function() {
        avataxModuleResources.synchronizeOrders({ orderIds: [$scope.blade.currentEntity.id] }, function(notification) {
            var newBlade = {
                id: 'avaTaxOrderSynchronizationProgress',
                notification: notification,
                controller: 'virtoCommerce.avataxModule.ordersSynchronizationProgressController',
                template: 'Modules/$(Avalara.Tax)/Scripts/blades/orders-synchronization-progress.tpl.html'
            };

            bladeNavigationService.showBlade(newBlade, $scope.blade);
        });
    }

    $scope.widget.refresh();
}]);