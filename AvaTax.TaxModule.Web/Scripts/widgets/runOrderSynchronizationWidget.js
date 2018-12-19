angular.module('virtoCommerce.avataxModule')
.controller('virtoCommerce.avataxModule.runOrderSynchronizationWidgetController', ['$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.avataxModule.resources', function ($scope, bladeNavigationService, avataxModuleResources) {
    $scope.widget.refresh = function () {
        $scope.background = "transparent";
        $scope.$watch(function(scope) { return scope.blade.currentEntity; },
            function(currentEntity) {
                if (currentEntity) {
                    updateWidgetContents(currentEntity.id);
                }
            });
    }

    function updateWidgetContents(orderId) {
        $scope.avaTaxMessage = 'avaTax.commands.run-order-synchronization.labels.waiting-for-status';
        $scope.avaTaxOrderStatus = null;
        $scope.avaTaxOrderStatusReceived = false;

        avataxModuleResources.getOrderSynchronizationStatus({ orderId: orderId },
            function (orderStatus) {
                $scope.avaTaxOrderStatus = orderStatus;

                if (!orderStatus.storeUsesAvaTax) {
                    $scope.avaTaxMessage = 'avaTax.commands.run-order-synchronization.labels.store-does-not-use-avatax';
                } else if (orderStatus.hasErrors) {
                    $scope.avaTaxMessage = 'avaTax.commands.run-order-synchronization.labels.error';
                } else if (!orderStatus.lastSynchronizationDate) {
                    $scope.avaTaxMessage = 'avaTax.commands.run-order-synchronization.labels.send-to-avatax';
                } else {
                    $scope.avaTaxOrderStatusReceived = true;
                    $scope.avaTaxMessage = 'avaTax.commands.run-order-synchronization.labels.sent-to-avatax';
                }

                updateWidgetColor(orderStatus);
            });
    }

    function updateWidgetColor(orderStatus) {
        var currentEntity = $scope.blade.currentEntity;
        if (orderStatus.lastSynchronizationDate >= currentEntity.modifiedDate) {
            $scope.background = "transparent";
        } else {
            $scope.background = "LightCoral";
        }
    }

    $scope.showDetails = function() {
        var newBlade = {
            id: 'detailChild',
            currentEntityId: $scope.blade.currentEntityId,
            currentEntity: $scope.blade.currentEntity,
            data: $scope.avaTaxOrderStatus,
            lastSynchronizationDate: $scope.avaTaxOrderStatus.lastSynchronizationDate,
            orderId: $scope.blade.currentEntity.id,
            parentRefresh: updateWidgetContents,
            controller: 'virtoCommerce.avataxModule.orderSynchronizationStatusController',
            template: 'Modules/$(Avalara.Tax)/Scripts/blades/order-synchronization-status.tpl.html'
        };
        bladeNavigationService.showBlade(newBlade, $scope.blade);
    }

    $scope.widget.refresh();
}]);