//Call this to register our module to main application
var moduleName = "virtoCommerce.avataxModule";

if (AppDependencies !== undefined) {
    AppDependencies.push(moduleName);
}

angular.module(moduleName, [])
    .factory('virtoCommerce.avataxModule.avaSettingsFactory', [function () {

        function loadAvaSettings(settings) {
            var serviceUrlSetting = findSettingByName(settings, "Avalara.Credentials.ServiceUrl");
            var companyCodeSetting = findSettingByName(settings, "Avalara.Credentials.CompanyCode");

            var result = null;
            if (serviceUrlSetting && companyCodeSetting) {
                result = {
                    serviceUrl: getCurrentValueOf(serviceUrlSetting),
                    companyCode: getCurrentValueOf(companyCodeSetting)
                };
            }

            return result;
        }

        function findSettingByName(settings, requiredName) {
            return _.find(settings, function (setting) {
                return setting.name === requiredName;
            });
        }

        function getCurrentValueOf(setting) {
            return setting.values[0].value;
        }
        return {
            loadAvaSettings: loadAvaSettings,
        };
    }])
    .run(
    ['$state', 'platformWebApp.toolbarService', 'virtoCommerce.avataxModule.resources', 'platformWebApp.widgetService', 'platformWebApp.authService', 'platformWebApp.pushNotificationTemplateResolver', 'platformWebApp.bladeNavigationService', function ($state, toolbarService, avaTaxApi, widgetService, authService, pushNotificationTemplateResolver, bladeNavigationService) {

            // Register widget on Avalara tax provider properties blade
            widgetService.registerWidget({
                    isVisible: function (blade) {
                        return blade.currentEntity && blade.currentEntity.name == 'Avalara taxes' && authService.checkPermission('tax:manage');
                    },
                    controller: 'virtoCommerce.avataxModule.testAvataxConnectionWidgetController',
                    template: 'Modules/$(VirtoCommerce.AvalaraTax)/Scripts/widgets/testAvataxConnectionWidget.tpl.html'
                },
                'taxProviderDetail');

            // Register widget in module settings
            widgetService.registerWidget({
                    isVisible: function (blade) {
                        return blade.currentEntities && blade.currentEntities['Avalara'];
                    },
                    controller: 'virtoCommerce.avataxModule.testAvataxConnectionWidgetController',
                    template: 'Modules/$(VirtoCommerce.AvalaraTax)/Scripts/widgets/testAvataxConnectionWidget.tpl.html'
                },
                'settingsDetail');

            // Register manual order synchronization widget on order details blade
            widgetService.registerWidget({
                    isVisible: function (blade) {
                        return blade.id && blade.id === 'orderDetail';
                    },
                    controller: 'virtoCommerce.avataxModule.runOrderSynchronizationWidgetController',
                    template: 'Modules/$(VirtoCommerce.AvalaraTax)/Scripts/widgets/runOrderSynchronizationWidget.tpl.html'
                },
                'customerOrderDetailWidgets');

            // Register order synchronization command (on the toolbar of the 'Customer orders' blade)
            var synchronizeOrdersCommand = {
                name: 'avaTax.commands.send-to-avatax',
                icon: 'fa fa-upload',
                index: 2,
                executeMethod: function(blade) {
                    var selectedOrders = blade.$scope.gridApi.selection.getSelectedRows();
                    var orderIds = _.pluck(selectedOrders, "id");
                    avaTaxApi.synchronizeOrders({ orderIds: orderIds }, function (notification) {
                        var newBlade = {
                            id: 'avaTaxOrderSynchronizationProgress',
                            notification: notification,
                            controller: 'virtoCommerce.avataxModule.ordersSynchronizationProgressController',
                            template: 'Modules/$(VirtoCommerce.AvalaraTax)/Scripts/blades/orders-synchronization-progress.tpl.html'
                        };

                        bladeNavigationService.showBlade(newBlade, blade);
                    });
                },
                canExecuteMethod: function(blade) {
                    return blade.$scope.gridApi && blade.$scope.gridApi.selection.getSelectedCount() > 0;
                },
                permission: 'tax:manage'
            };
            toolbarService.register(synchronizeOrdersCommand, 'virtoCommerce.orderModule.customerOrderListController');

            var menuNotificationTemplate =
            {
                priority: 901,
                satisfy: function (notify, place) { return place == 'menu' && notify.notifyType == 'AvaTaxOrdersSynchronization'; },
                template: 'Modules/$(VirtoCommerce.AvalaraTax)/Scripts/notifications/ordersSynchronization-menu.tpl.html',
                action: function (notify) { $state.go('workspace.pushNotificationsHistory', notify) }
            };
            pushNotificationTemplateResolver.register(menuNotificationTemplate);

            var historyNotificationTemplate =
            {
                priority: 901,
                satisfy: function (notify, place) { return place == 'history' && notify.notifyType == 'AvaTaxOrdersSynchronization'; },
                template: 'Modules/$(VirtoCommerce.AvalaraTax)/Scripts/notifications/ordersSynchronization-history.tpl.html',
                action: function (notify) {
                    var blade = {
                        id: 'avaTaxOrdersSynchronizationProgress',
                        title: 'Title1',
                        subtitle: 'Subtitle1',
                        template: 'Modules/$(VirtoCommerce.AvalaraTax)/Scripts/blades/orders-synchronization-progress.tpl.html',
                        controller: 'virtoCommerce.avataxModule.ordersSynchronizationProgressController',
                        notification: notify
                    };
                    bladeNavigationService.showBlade(blade);
                }
            };
            pushNotificationTemplateResolver.register(historyNotificationTemplate);
        }])
    ;
