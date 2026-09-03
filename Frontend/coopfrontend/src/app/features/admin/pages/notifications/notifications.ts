import { ChangeDetectionStrategy, Component } from '@angular/core';

import { NotificationsPageComponent } from '../../../../shared/components';

@Component({
  selector: 'app-admin-notifications',
  imports: [NotificationsPageComponent],
  template: `<coop-notifications-page />`,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NotificationsComponent {}
