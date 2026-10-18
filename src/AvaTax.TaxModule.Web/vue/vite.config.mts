import path from "node:path";
import { getDynamicModuleConfiguration } from "@vc-shell/mf-module";

// Avalara tax widgets on the tax provider, settings and order blades: a Module Federation remote of the Admin Shell host.
// Output: <module>/plugins/admin-shell/ (remoteEntry.js + public/plugin.json),
// served by the platform at /modules/$(VirtoCommerce.AvalaraTax)/plugins/admin-shell/.
const base = "/modules/$(VirtoCommerce.AvalaraTax)/plugins/admin-shell/";

export default getDynamicModuleConfiguration({
  appId: "admin-shell",
  moduleRoot: path.resolve(__dirname, ".."),
  entry: "./src/modules/index.ts",
  remoteName: "avalara_tax_admin",
  base,
});
