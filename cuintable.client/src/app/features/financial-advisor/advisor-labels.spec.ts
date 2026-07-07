import { categoryKey, reasonKeys, statusKey, typeKey } from './advisor-labels';

describe('advisor-labels', () => {
  it('maps categories to translation keys', () => {
    expect(categoryKey(0)).toBe('ADVISOR.CAT_DELIVERY');
    expect(categoryKey(3)).toBe('ADVISOR.CAT_CONVENIENCE');
    expect(categoryKey(15)).toBe('ADVISOR.CAT_OTHER');
    expect(categoryKey(16)).toBe('ADVISOR.CAT_TRANSFERS');
  });

  it('falls back to Other for unknown categories', () => {
    expect(categoryKey(99)).toBe('ADVISOR.CAT_OTHER');
  });

  it('maps transaction types and statement statuses', () => {
    expect(typeKey(1)).toBe('ADVISOR.TYPE_PAYMENT');
    expect(statusKey(2)).toBe('ADVISOR.STATUS_COMPLETED');
    expect(statusKey(3)).toBe('ADVISOR.STATUS_FAILED');
  });

  it('splits comma-joined suspicious reason codes', () => {
    expect(reasonKeys('DUPLICATE,FOREIGN')).toEqual([
      'ADVISOR.REASON_DUPLICATE',
      'ADVISOR.REASON_FOREIGN',
    ]);
  });

  it('returns an empty list when there is no reason', () => {
    expect(reasonKeys(null)).toEqual([]);
    expect(reasonKeys('')).toEqual([]);
  });
});
