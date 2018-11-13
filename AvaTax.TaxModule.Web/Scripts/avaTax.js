//Call this to register our module to main application
var moduleName = "virtoCommerce.avataxModule";

if (AppDependencies != undefined) {
    AppDependencies.push(moduleName);
}

angular.module(moduleName, [
])

.run(
  ['platformWebApp.toolbarService', 'platformWebApp.dialogService', 'virtoCommerce.avataxModule.resources', 'platformWebApp.mainMenuService', 'platformWebApp.widgetService', 'platformWebApp.authService', function (toolbarService, dialogService, avataxModuleResources, mainMenuService, widgetService, authService) {

      //Register widgets in AvaTax module properties
      widgetService.registerWidget({
              isVisible: function(blade) {
                   return blade.currentEntity.id == 'Avalara.Tax' && authService.checkPermission('tax:manage');
              },
              controller: 'virtoCommerce.avataxModule.avataxWidgetController',
              template: 'Modules/$(Avalara.Tax)/Scripts/widgets/avataxWidget.tpl.html'
          },
          'moduleDetail');

      // Also registering the same widget on AvaTax tax provider properties blade
      widgetService.registerWidget({
              isVisible: function(blade) {
                  return blade.currentEntity.name == 'Avalara taxes' && authService.checkPermission('tax:manage');
              },
              controller: 'virtoCommerce.avataxModule.avataxWidgetController',
              template: 'Modules/$(Avalara.Tax)/Scripts/widgets/avataxWidget.tpl.html'
          },
          'taxProviderDetail');
  }])
;