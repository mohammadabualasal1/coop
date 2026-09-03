import { Pipe, PipeTransform } from '@angular/core';

export type CoopDateMode = 'date' | 'datetime';

const DATE_FORMATTER = new Intl.DateTimeFormat('ar-JO-u-nu-latn', {
  year: 'numeric',
  month: '2-digit',
  day: '2-digit'
});

const DATETIME_FORMATTER = new Intl.DateTimeFormat('ar-JO-u-nu-latn', {
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  hour12: false
});

function partValue(parts: Intl.DateTimeFormatPart[], type: Intl.DateTimeFormatPartTypes): string {
  return parts.find((part) => part.type === type)?.value ?? '';
}

@Pipe({
  name: 'coopDate',
  standalone: true
})
export class CoopDatePipe implements PipeTransform {
  transform(value: string | Date | null | undefined, mode: CoopDateMode = 'date'): string {
    if (!value) {
      return '';
    }

    const date = value instanceof Date ? value : new Date(value);

    if (Number.isNaN(date.getTime())) {
      return '';
    }

    const formatter = mode === 'datetime' ? DATETIME_FORMATTER : DATE_FORMATTER;
    const parts = formatter.formatToParts(date);

    const datePart = `${partValue(parts, 'year')}/${partValue(parts, 'month')}/${partValue(parts, 'day')}`;

    if (mode === 'date') {
      return datePart;
    }

    return `${datePart} ${partValue(parts, 'hour')}:${partValue(parts, 'minute')}`;
  }
}
