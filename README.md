# Avalara.Tax
Avalara.Tax module provides real time integration with Avalara Tax automation. This module is officially <a href="https://www.avalara.com/integrations/virto-commerce" target="_blank">certified by Avalara</a> to be compatible with Avalara API.

![Avalara Tax UI](https://cloud.githubusercontent.com/assets/5801549/16107931/729581a8-33a8-11e6-9374-fde0b0233d94.png)

# Documentation
User guide: <a href="https://virtocommerce.com/docs/vc2userguide/order-management/working-with-taxes" target="_blank">Working with taxes in Avalara tax module</a>.

# Installation
Installing the module:
* Automatically: in VC Manager go to Configuration -> Modules -> Avalara tax -> Install
* Manually: download module zip package from https://github.com/VirtoCommerce/vc-module-avatax/releases. In VC Manager go to Configuration -> Modules -> Advanced -> upload module package -> Install

# Settings
* **Avalara.Tax.Credentials.AccountNumber** - Account number provided by Avalara during registration process
* **Avalara.Tax.Credentials.LicenseKey** - Account License Key provided by Avalara during registration process
* **Avalara.Tax.Credentials.CompanyCode** - Company code that should match with the code provided to the company registered in Avalara admin manager
* **Avalara.Tax.Credentials.ServiceUrl** - Link to Avalara API service
* **Avalara.Tax.IsEnabled** - Enable or disable tax calculation
* **Avalara.Tax.IsValidateAddress** - Enable or disable address validation

# License
Copyright (c) Virto Solutions LTD.  All rights reserved.

Licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at

http://virtocommerce.com/opensourcelicense

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied.
