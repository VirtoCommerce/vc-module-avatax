angular.module('virtoCommerce.avataxModule')
.controller('virtoCommerce.avataxModule.runOrderSynchronizationWidgetController', ['$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.avataxModule.resources', function ($scope, bladeNavigationService, avataxModuleResources) {
    $scope.widget.refresh = function () {
        $scope.$watch(function(scope) { return scope.blade.currentEntity; },
            function(currentEntity) {
                if (currentEntity) {
                    updateWidgetContents(currentEntity.id);
                }
            });
    }

    function updateWidgetContents(orderId) {
        $scope.avaTaxMessage = 'avatax.commands.run-order-synchronization.labels.waiting-for-status';
        $scope.avaTaxOrderStatus = null;
        $scope.avaTaxOrderStatusReceived = false;

        avataxModuleResources.getOrderSynchronizationStatus({ orderId: orderId },
            function (orderStatus) {
                $scope.avaTaxOrderStatus = orderStatus;

                if (!orderStatus.storeUsesAvaTax) {
                    $scope.avaTaxMessage = 'avatax.commands.run-order-synchronization.labels.store-does-not-use-avatax';
                } else if (orderStatus.hasErrors) {
                    $scope.avaTaxMessage = 'avatax.commands.run-order-synchronization.labels.error';
                } else {
                    $scope.avaTaxOrderStatusReceived = true;
                    $scope.avaTaxMessage = 'avatax.commands.run-order-synchronization.labels.sent-to-avatax';
                }
            });
    }

    $scope.runOrderSynchronization = function() {
        avataxModuleResources.synchronizeOrders({ orderIds: [$scope.blade.currentEntity.id] }, function(notification) {
            var newBlade = {
                id: 'avaTaxOrderSynchronizationProgress',
                notification: notification,
                controller: 'virtoCommerce.avataxModule.ordersSynchronizationProgressController',
                template: 'Modules/$(Avalara.Tax)/Scripts/blades/orders-synchronization-progress.tpl.html'
            };

            $scope.$on("new-notification-event", function (event, notification) {
                if (notification && notification.id == newBlade.notification.id && notification.finished != null) {
                    updateWidgetContents($scope.blade.currentEntity.id);
                }
            });

            bladeNavigationService.showBlade(newBlade, $scope.blade);
        });
    }

    $scope.widget.refresh();
}]);