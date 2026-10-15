// The Orders list's toolbar extension point "orders-list-toolbar" (contract documented in the Orders module,
// vc-module-order .../vue/src/modules/pages/list.vue). Types repeated here: the contract is by shape.

export interface OrdersListToolbarContext {
  selection: { id?: string }[];
  openBlade: (event: { name: string; param?: string; options?: Record<string, unknown> }) => Promise<void>;
  reload: () => Promise<void>;
}

export interface OrdersListToolbarCommand {
  title: string;
  icon?: string;
  permissions?: string | string[];
  isVisible?: (ctx: OrdersListToolbarContext) => boolean;
  disabled?: (ctx: OrdersListToolbarContext) => boolean;
  onClick: (ctx: OrdersListToolbarContext) => unknown;
}
