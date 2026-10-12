import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { SessionService } from '@core/session.service';

export const guestGuard: CanMatchFn = () =>
  !inject(SessionService).isAuthenticated() || inject(Router).createUrlTree(['/']);
