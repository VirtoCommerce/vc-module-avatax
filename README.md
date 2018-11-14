# Avalara.Tax
Avalara.Tax module provides real time integration with Avalara Tax automation. This module is officially <a href="https://www.avalara.com/integrations/virto-commerce" target="_blank">certified by Avalara</a> to be compatible with Avalara API.

![Avalara Tax UI](https://user-images.githubusercontent.com/1835759/48475050-84442c00-e82e-11e8-899f-10452b382ec1.png)

# Documentation
User guide: <a href="https://virtocommerce.com/docs/vc2userguide/order-management/working-with-taxes" target="_blank">Working with taxes in Avalara tax module</a>.

# Installation
Installing the module:
* Automatically: in VC Manager go to Configuration -> Modules -> Avalara tax -> Install
* Manually: 
    * Download module zip package from https://github.com/VirtoCommerce/vc-module-avatax/releases. 
    * Open the VC Platform Manager, go to Configuration -> Modules -> Advanced -> upload module package -> Install and choose the package downloaded on previous step.

# Settings
## Avalara connection settings
The module can be configured in two places:
* Platform-wide settings: Settings -> Taxes -> Avalara or Modules -> Installed -> Avalara tax -> Settings
* Store-specific settings: Stores -> (your store) -> Tax providers -> Avalara taxes

Both of these places have the following settings:
* **Avalara.Tax.Credentials.AccountNumber** - Account number provided by Avalara during registration process
* **Avalara.Tax.Credentials.LicenseKey** - Account License Key provided by Avalara during registration process
* **Avalara.Tax.Credentials.CompanyCode** - Company code that should match with the code provided to the company registered in Avalara admin manager
* **Avalara.Tax.Credentials.ServiceUrl** - Link to Avalara API service:
    * `https://sandbox-rest.avatax.com` for the development environment;
    * `https://rest.avatax.com` for the production environment.
* **Avalara.Tax.IsEnabled** - Enable or disable tax calculation

Also, both of these blades have the "Test connection with AvaTax" widget. You can verify that the connection to Avalara Tax API will work with your current settings (even before saving the changes) by clicking this widget:
* If the test passed, the widget will turn green. this means that currently entered credentials and the service URL are correct, and these settings can be safely saved.

    ![Test connection widget, success](https://user-images.githubusercontent.com/1835759/48472743-822b9e80-e829-11e8-95f5-19d87ff04ae0.png)
* Otherwise, the widget will turn red, and the error status will appear in the blade header. You can click the "See details" link to see the explanation of the error.

    ![Test connection widget, error message](https://user-images.githubusercontent.com/1835759/48473017-1eee3c00-e82a-11e8-8489-a08ab261ce01.png)

> Note: if the tax calculation is disabled (**Avalara.Tax.IsEnabled** is turned off), the Avalara tax provider will ignore any tax requests, and this may lead to incorrect tax calculation. To help you prevent this, the connection test will fail if the tax calculation is turned off, even if the service URL and Avalara credentials are correct.
> ![Test connection widget, failure due to disabled tax calculation](https://user-images.githubusercontent.com/1835759/48472424-d6824e80-e828-11e8-9f27-c4c555f5abcf.png)

## Configuring tax types
Cart/order items should be assigned to tax category in order to calculate taxes correctly. That can be done by applying tax codes to the catalog items. That is called "Tax type" in VirtoCommerce platform. If none of the codes assigned to the item Avalara will calculate taxes by applying default code. So if that is the right choice in your case, you can leave "Tax Type" property value blank. Otherwise define available tax types in general settings of VirtoCommerce platform and apply appropriate types to the items. Note that you can apply tax type to the whole category of items. In that case all items in particular category and in nested subcategories will have the selected tax type code.

The tax type can be selected in the following locations:
* Category: Catalogs -> (your catalog) -> (your category) -> Manage -> Tax type;
* Item: Catalogs -> (your catalog) -> (your category) -> (your item) -> Tax type;
* Shipping method: Stores -> (your store) -> Shipping methods -> (your shipping method) -> Tax type.

> Note that the available tax types can be configured in VC Platform settings: Settings -> Commerce -> General -> Tax types.

## Tax exemption
This module can also provide the exemption number for selected customers to the Avalara Tax API. To configure it, follow these steps:
1. Open the customer details for the customer you want to configure exemption for;
2. Open dynamic properties for that customer;
3. Add the dynamic property named `Tax exempt` and select the `ShortText` type.
4. Fill the exemption certificate number to the value of this property.

# License
Copyright (c) Virtosoftware Ltd.  All rights reserved.

Licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at

http://virtocommerce.com/opensourcelicense

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied.
