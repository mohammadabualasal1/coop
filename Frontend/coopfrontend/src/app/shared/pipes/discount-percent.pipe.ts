import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'discountPercent',
  standalone: true
})
export class DiscountPercentPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    if (value === null || value === undefined) {
      return '';
    }

    const rounded = Math.round(value * 10) / 10;
    return rounded % 1 === 0 ? rounded.toFixed(0) : rounded.toFixed(1);
  }
}
