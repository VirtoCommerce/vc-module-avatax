angular.module('virtoCommerce.avataxModule')
    .controller('virtoCommerce.avataxModule.orderSynchronizationStatusController', ['$scope', 'platformWebApp.bladeNavigationService', 'virtoCommerce.avataxModule.resources', function ($scope, bladeNavigationService, avaTaxApi) {
    var blade = $scope.blade;

    blade.initialize = function (data) {
        blade.currentEntity = data;
        blade.hasData = data && data.storeUsesAvaTax && data.lastSynchronizationDate;
        blade.isLoading = false;
    };

    blade.headIcon = 'fa-file-text';


    blade.toolbarCommands = [
        {
            name: "avaTax.commands.send-to-avatax",
            icon: 'fa fa-upload',
            index: 0,
            executeMethod: function (blade) {
                avaTaxApi.synchronizeOrders({ orderIds: [blade.orderId] }, function (notification) {
                    var newBlade = {
                        id: 'avaTaxOrderSynchronizationProgress',
                        notification: notification,
                        controller: 'virtoCommerce.avataxModule.ordersSynchronizationProgressController',
                        template: 'Modules/$(Avalara.Tax)/Scripts/blades/orders-synchronization-progress.tpl.html'
                    };

                    $scope.$on("new-notification-event", function (event, notification) {
                        if (notification && notification.id == newBlade.notification.id && notification.finished != null) {
                            blade.parentRefresh(blade.orderId);
                        }
                    });

                    bladeNavigationService.showBlade(newBlade, $scope.blade);
                });
            },
            canExecuteMethod: function () { return true; },
            permission: 'tax:manage'
        }
    ];

    blade.title = 'avaTax.blades.order-synchronization-status.title';
    blade.subtitle = 'avaTax.blades.order-synchronization-status.subtitle';
    if (!blade.data) {
        blade.title = 'avaTax.blades.order-synchronization-status.title-new';
        blade.subtitle = undefined;
    }

    blade.formatLastSynchronizationDate = function() {
        if (blade.lastSynchronizationDate) {
            return new Date(blade.lastSynchronizationDate).toLocaleString();
        } else {
            return '';
        }
    }

    blade.initialize(blade.data);
}]);
