// Translation-key maps for the numeric enums the API returns.
// Pure functions so they stay unit-testable.

const CATEGORY_KEYS = [
  'CAT_DELIVERY', // 0 ComidaDomicilio
  'CAT_RESTAURANTS', // 1 RestaurantesCafes
  'CAT_GROCERIES', // 2 Supermercado
  'CAT_CONVENIENCE', // 3 TiendaConveniencia
  'CAT_SUBSCRIPTIONS', // 4 Suscripciones
  'CAT_TRANSPORT', // 5 Transporte
  'CAT_GAS', // 6 Gasolina
  'CAT_ONLINE', // 7 ComprasOnline
  'CAT_HEALTH', // 8 Salud
  'CAT_TRAVEL', // 9 Viajes
  'CAT_ENTERTAINMENT', // 10 Entretenimiento
  'CAT_UTILITIES', // 11 Servicios
  'CAT_FEES', // 12 ComisionesIntereses
  'CAT_PAYMENTS', // 13 PagosAbonos
  'CAT_CASH', // 14 RetiroEfectivo
  'CAT_OTHER', // 15 Otro
  'CAT_TRANSFERS', // 16 Transferencias
];

const TYPE_KEYS = ['TYPE_CHARGE', 'TYPE_PAYMENT', 'TYPE_FEE', 'TYPE_INTEREST', 'TYPE_REFUND'];

const STATUS_KEYS = ['STATUS_UPLOADED', 'STATUS_PROCESSING', 'STATUS_COMPLETED', 'STATUS_FAILED'];

export function categoryKey(category: number): string {
  return `ADVISOR.${CATEGORY_KEYS[category] ?? 'CAT_OTHER'}`;
}

export function typeKey(type: number): string {
  return `ADVISOR.${TYPE_KEYS[type] ?? 'TYPE_CHARGE'}`;
}

export function statusKey(status: number): string {
  return `ADVISOR.${STATUS_KEYS[status] ?? 'STATUS_UPLOADED'}`;
}

/** "DUPLICATE,FOREIGN" -> ['ADVISOR.REASON_DUPLICATE', 'ADVISOR.REASON_FOREIGN'] */
export function reasonKeys(suspiciousReason: string | null): string[] {
  if (!suspiciousReason) return [];
  return suspiciousReason
    .split(',')
    .filter((r) => r.length > 0)
    .map((r) => `ADVISOR.REASON_${r.trim()}`);
}
