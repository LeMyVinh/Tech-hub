import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const session = auth.restoreSession();

  if (session?.token && !req.url.includes('/auth/')) {
    const cloned = req.clone({
      setHeaders: { Authorization: `Bearer ${session.token}` },
    });
    return next(cloned);
  }

  return next(req);
};
