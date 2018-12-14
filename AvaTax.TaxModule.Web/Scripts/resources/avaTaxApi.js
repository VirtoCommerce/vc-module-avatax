angular.module('virtoCommerce.avataxModule')
    .factory('virtoCommerce.avataxModule.resources', ['$resource', function ($resource) {

        return $resource(null, null, {
            ping: { url: 'api/tax/avatax/ping', method: 'POST' },
            synchronizeOrders: { url: 'api/tax/avatax/orders/synchronize', method: 'POST' },
            cancelSynchronization: { url: 'api/tax/avatax/orders/:jobId/cancel', method: 'POST' },
            getOrderSynchronizationStatus: { url: 'api/tax/avatax/orders/:orderId/status', method: 'GET' }
        });
    }]);
