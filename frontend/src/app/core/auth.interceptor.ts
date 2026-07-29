import { HttpErrorResponse, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { BehaviorSubject, Observable, catchError, filter, switchMap, take, throwError } from 'rxjs';
import { AuthService } from '../auth.service';

let isRefreshing = false;
const refreshedToken$ = new BehaviorSubject<string | null>(null);

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const session = auth.restoreSession();
  const isAuthUrl = req.url.includes('/auth/');

  const authReq =
    session?.token && !isAuthUrl
      ? req.clone({ setHeaders: { Authorization: `Bearer ${session.token}` } })
      : req;

  return next(authReq).pipe(
    catchError(err => {
      if (err instanceof HttpErrorResponse && err.status === 401 && !isAuthUrl) {
        return handle401(authReq, next, auth);
      }
      return throwError(() => err);
    })
  );
};

function handle401(
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
  auth: AuthService
): Observable<any> {
  const session = auth.restoreSession();

  if (!session?.refreshToken) {
    auth.forceLogout();
    return throwError(() => new Error('Session expired.'));
  }

  if (!isRefreshing) {
    isRefreshing = true;
    refreshedToken$.next(null);

    return auth.refresh(session.refreshToken).pipe(
      switchMap(newSession => {
        isRefreshing = false;
        auth.saveSession(newSession);
        refreshedToken$.next(newSession.token);

        const retryReq = req.clone({
          setHeaders: { Authorization: `Bearer ${newSession.token}` },
        });
        return next(retryReq);
      }),
      catchError(refreshErr => {
        isRefreshing = false;
        auth.forceLogout();
        return throwError(() => refreshErr);
      })
    );
  }

  return refreshedToken$.pipe(
    filter((token): token is string => token !== null),
    take(1),
    switchMap(token => {
      const retryReq = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
      return next(retryReq);
    })
  );
}