﻿angular.module('virtoCommerce.avataxModule')
    .factory('virtoCommerce.avataxModule.resources', ['$resource', function ($resource) {

        return $resource(null, null, {
            ping: { url: 'api/tax/avatax/ping', method: 'POST' }
        });
    }]);
