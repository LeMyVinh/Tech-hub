import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const session = auth.restoreSession();

  if (!session) {
    router.navigate(['/auth/login']);
    return false;
  }

  return true;
};

export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const session = auth.restoreSession();

  if (!session || session.user.role !== 'Admin') {
    router.navigate(['/catalog']);
    return false;
  }

  return true;
};
