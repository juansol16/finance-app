import { describe, expect, it } from 'vitest';
import {
  buildMonthCells,
  clampDay,
  computeFloat,
  nextOccurrence,
  rankCardsByFloat,
} from './card-schedule';
import { CreditCard } from '../../core/services/credit-card.service';

const card = (overrides: Partial<CreditCard>): CreditCard => ({
  id: 'x',
  bank: 'BBVA',
  nickname: 'Azul',
  lastFourDigits: '1234',
  cutoffDay: null,
  paymentDueDay: null,
  isActive: true,
  createdAt: '2026-01-01',
  ...overrides,
});

describe('clampDay', () => {
  it('keeps days that exist in the month', () => {
    expect(clampDay(15, 2026, 1)).toBe(15);
  });

  it('clamps day 31 to the last day of February', () => {
    expect(clampDay(31, 2026, 1)).toBe(28);
    expect(clampDay(31, 2028, 1)).toBe(29); // leap year
  });
});

describe('nextOccurrence', () => {
  it('finds the day later in the same month', () => {
    const d = nextOccurrence(20, new Date(2026, 6, 6), false);
    expect(d).toEqual(new Date(2026, 6, 20));
  });

  it('rolls to next month when the day already passed', () => {
    const d = nextOccurrence(5, new Date(2026, 6, 6), false);
    expect(d).toEqual(new Date(2026, 7, 5));
  });

  it('returns today only when inclusive', () => {
    const today = new Date(2026, 6, 6);
    expect(nextOccurrence(6, today, true)).toEqual(today);
    expect(nextOccurrence(6, today, false)).toEqual(new Date(2026, 7, 6));
  });
});

describe('computeFloat', () => {
  it('purchase right after the cut-off gets the longest float', () => {
    // corte day 5, pago day 25; buying on the 6th closes Aug 5, paid Aug 25
    const f = computeFloat(5, 25, new Date(2026, 6, 6));
    expect(f.statementClose).toEqual(new Date(2026, 7, 5));
    expect(f.paymentDate).toEqual(new Date(2026, 7, 25));
    expect(f.floatDays).toBe(50);
  });

  it('purchase on the cut-off day is counted into the closing statement', () => {
    const f = computeFloat(6, 26, new Date(2026, 6, 6));
    expect(f.statementClose).toEqual(new Date(2026, 6, 6));
    expect(f.paymentDate).toEqual(new Date(2026, 6, 26));
    expect(f.floatDays).toBe(20);
  });

  it('payment day earlier in the month rolls past month end', () => {
    // corte 25, pago 15 => statement closing Jul 25 is paid Aug 15
    const f = computeFloat(25, 15, new Date(2026, 6, 6));
    expect(f.statementClose).toEqual(new Date(2026, 6, 25));
    expect(f.paymentDate).toEqual(new Date(2026, 7, 15));
    expect(f.floatDays).toBe(40);
  });
});

describe('rankCardsByFloat', () => {
  const today = new Date(2026, 6, 6);

  it('puts the card whose cut-off just passed first', () => {
    const justCut = card({ id: 'a', nickname: 'JustCut', cutoffDay: 5, paymentDueDay: 25 });
    const aboutToCut = card({ id: 'b', nickname: 'AboutToCut', cutoffDay: 8, paymentDueDay: 28 });
    const ranked = rankCardsByFloat([aboutToCut, justCut], today);
    expect(ranked[0].card.nickname).toBe('JustCut');
    expect(ranked[0].floatDays).toBe(50);
    expect(ranked[1].floatDays).toBe(22);
  });

  it('excludes inactive cards and cards without a cut-off day', () => {
    const inactive = card({ id: 'a', cutoffDay: 5, paymentDueDay: 25, isActive: false });
    const noDays = card({ id: 'b' });
    expect(rankCardsByFloat([inactive, noDays], today)).toEqual([]);
  });

  it('ranks cards without a payment day last, with null float', () => {
    const full = card({ id: 'a', cutoffDay: 5, paymentDueDay: 25 });
    const corteOnly = card({ id: 'b', nickname: 'CorteOnly', cutoffDay: 4 });
    const ranked = rankCardsByFloat([corteOnly, full], today);
    expect(ranked[1].card.nickname).toBe('CorteOnly');
    expect(ranked[1].floatDays).toBeNull();
    expect(ranked[1].paymentDate).toBeNull();
  });
});

describe('buildMonthCells', () => {
  it('pads to a Monday-first grid and marks today', () => {
    // July 2026 starts on a Wednesday => 2 leading blanks
    const cells = buildMonthCells(2026, 6, [], new Date(2026, 6, 6));
    expect(cells.slice(0, 2).every((c) => c.day === 0)).toBe(true);
    expect(cells.length).toBe(2 + 31);
    expect(cells.find((c) => c.isToday)?.day).toBe(6);
  });

  it('marks cut-off and payment days with card nicknames, clamped to month end', () => {
    const c = card({ nickname: 'Oro', cutoffDay: 31, paymentDueDay: 10 });
    const feb = buildMonthCells(2026, 1, [c], new Date(2026, 1, 1));
    expect(feb.find((x) => x.day === 28)?.cutoffCards).toEqual(['Oro']);
    expect(feb.find((x) => x.day === 10)?.paymentCards).toEqual(['Oro']);
  });

  it('ignores inactive cards', () => {
    const c = card({ cutoffDay: 15, isActive: false });
    const cells = buildMonthCells(2026, 6, [c], new Date(2026, 6, 6));
    expect(cells.find((x) => x.day === 15)?.cutoffCards).toEqual([]);
  });
});
