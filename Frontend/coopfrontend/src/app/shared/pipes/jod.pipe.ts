import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'jod',
  standalone: true
})
export class JodPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    if (value === null || value === undefined) {
      return '';
    }

    return `${value.toFixed(2)} د.أ`;
  }
}
