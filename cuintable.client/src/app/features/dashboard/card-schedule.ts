import { CreditCard } from '../../core/services/credit-card.service';

/**
 * Statement-cycle math for credit cards. Cut-off and payment days are
 * days-of-month (1-31); a day beyond a month's end falls on its last day.
 */

export interface CardScheduleEntry {
  card: CreditCard;
  /** Statement close that a purchase made today would land on. */
  statementClose: Date;
  /** When that statement is actually paid (null if the card has no payment day). */
  paymentDate: Date | null;
  /** Days between today and paymentDate (null if no payment day). */
  floatDays: number | null;
}

export interface CalendarCell {
  /** 1-31, or 0 for leading padding cells. */
  day: number;
  isToday: boolean;
  cutoffCards: string[];
  paymentCards: string[];
}

export function clampDay(day: number, year: number, month0: number): number {
  const daysInMonth = new Date(year, month0 + 1, 0).getDate();
  return Math.min(day, daysInMonth);
}

/** First date whose day-of-month is `day`, strictly after `from` (or on it when inclusive). */
export function nextOccurrence(day: number, from: Date, inclusive: boolean): Date {
  const base = new Date(from.getFullYear(), from.getMonth(), from.getDate());
  for (let offset = 0; ; offset++) {
    const first = new Date(base.getFullYear(), base.getMonth() + offset, 1);
    const candidate = new Date(
      first.getFullYear(),
      first.getMonth(),
      clampDay(day, first.getFullYear(), first.getMonth()),
    );
    if (candidate > base || (inclusive && candidate.getTime() === base.getTime())) {
      return candidate;
    }
  }
}

/**
 * Float for a purchase made today: it lands on the statement closing at the
 * first cut-off on/after today (a purchase on the cut-off day itself is
 * conservatively counted into the closing statement), and is paid on the
 * first payment day after that close.
 */
export function computeFloat(
  cutoffDay: number,
  paymentDueDay: number,
  today: Date,
): { statementClose: Date; paymentDate: Date; floatDays: number } {
  const base = new Date(today.getFullYear(), today.getMonth(), today.getDate());
  const statementClose = nextOccurrence(cutoffDay, base, true);
  const paymentDate = nextOccurrence(paymentDueDay, statementClose, false);
  const floatDays = Math.round((paymentDate.getTime() - base.getTime()) / 86_400_000);
  return { statementClose, paymentDate, floatDays };
}

/** Active cards with a cut-off day, best float first; cards missing a payment day rank last. */
export function rankCardsByFloat(cards: CreditCard[], today: Date): CardScheduleEntry[] {
  return cards
    .filter((c) => c.isActive && c.cutoffDay)
    .map((c): CardScheduleEntry => {
      if (!c.paymentDueDay) {
        return {
          card: c,
          statementClose: nextOccurrence(c.cutoffDay!, today, true),
          paymentDate: null,
          floatDays: null,
        };
      }
      return { card: c, ...computeFloat(c.cutoffDay!, c.paymentDueDay, today) };
    })
    .sort((a, b) => (b.floatDays ?? -1) - (a.floatDays ?? -1));
}

/** Monday-first month grid with each card's cut-off/payment days marked. */
export function buildMonthCells(
  year: number,
  month0: number,
  cards: CreditCard[],
  today: Date,
): CalendarCell[] {
  const active = cards.filter((c) => c.isActive);
  const daysInMonth = new Date(year, month0 + 1, 0).getDate();
  const leadingBlanks = (new Date(year, month0, 1).getDay() + 6) % 7;

  const cells: CalendarCell[] = [];
  for (let i = 0; i < leadingBlanks; i++) {
    cells.push({ day: 0, isToday: false, cutoffCards: [], paymentCards: [] });
  }
  for (let d = 1; d <= daysInMonth; d++) {
    cells.push({
      day: d,
      isToday: today.getFullYear() === year && today.getMonth() === month0 && today.getDate() === d,
      cutoffCards: active
        .filter((c) => c.cutoffDay && clampDay(c.cutoffDay, year, month0) === d)
        .map((c) => c.nickname),
      paymentCards: active
        .filter((c) => c.paymentDueDay && clampDay(c.paymentDueDay, year, month0) === d)
        .map((c) => c.nickname),
    });
  }
  return cells;
}
