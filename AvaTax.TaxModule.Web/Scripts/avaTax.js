//Call this to register our module to main application
var moduleName = "virtoCommerce.avataxModule";

if (AppDependencies != undefined) {
    AppDependencies.push(moduleName);
}

angular.module(moduleName, [
])

.run(
  ['platformWebApp.toolbarService', 'platformWebApp.dialogService', 'virtoCommerce.avataxModule.resources', 'platformWebApp.mainMenuService', 'platformWebApp.widgetService', 'platformWebApp.authService', function (toolbarService, dialogService, avataxModuleResources, mainMenuService, widgetService, authService) {

      // Register widget on Avalara tax provider properties blade
      widgetService.registerWidget({
              isVisible: function(blade) {
                  return blade.currentEntity.name == 'Avalara taxes' && authService.checkPermission('tax:manage');
              },
              controller: 'virtoCommerce.avataxModule.testStoreSettingsWidgetController',
              template: 'Modules/$(Avalara.Tax)/Scripts/widgets/avataxWidget.tpl.html'
          },
          'taxProviderDetail');

      // Register widget in module settings
      widgetService.registerWidget({
              isVisible: function(blade) {
                  return blade.currentEntities && blade.currentEntities['Avalara'];
              },
              controller: 'virtoCommerce.avataxModule.testPlatformSettingsWidgetController',
              template: 'Modules/$(Avalara.Tax)/Scripts/widgets/avataxWidget.tpl.html'
          },
          'settingsDetail');
  }])
;