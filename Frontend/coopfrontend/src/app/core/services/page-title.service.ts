import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Injectable, inject, signal } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class PageTitleService {
  private readonly router = inject(Router);
  private readonly activatedRoute = inject(ActivatedRoute);

  readonly title = signal('');

  constructor() {
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed()
      )
      .subscribe(() => this.refresh());
  }

  refresh(): void {
    let route = this.activatedRoute;

    while (route.firstChild) {
      route = route.firstChild;
    }

    this.title.set((route.snapshot?.data?.['title'] as string) ?? '');
  }
}
