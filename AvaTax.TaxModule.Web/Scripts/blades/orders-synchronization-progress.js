angular.module('virtoCommerce.avataxModule')
    .controller('virtoCommerce.avataxModule.ordersSynchronizationProgressController', ['$scope', 'virtoCommerce.avataxModule.resources', function ($scope, avaTaxApiApi) {
        var blade = $scope.blade;

        $scope.$on("new-notification-event", function (event, notification) {
            if (blade.notification && notification.id == blade.notification.id) {
                angular.copy(notification, blade.notification);
            }
        });

        blade.toolbarCommands = [{
            name: 'platform.commands.cancel',
            icon: 'fa fa-times',
            canExecuteMethod: function () {
                return blade.notification && !blade.notification.finished;
            },
            executeMethod: function () {
                taskApi.cancelSynchronization({ jobId: blade.notification.jobId }, null);
            }
        }];

        blade.title = blade.notification.title;
        blade.headIcon = 'fa-file-text';
        blade.isLoading = false;
    }]);
