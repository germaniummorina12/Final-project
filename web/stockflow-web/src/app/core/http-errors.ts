import { HttpErrorResponse } from '@angular/common/http';

export function messageFrom(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'Something went wrong.';
  }
  if (error.status === 0) {
    return 'Cannot reach the server. Is the API running?';
  }

  const body = error.error as
    | { detail?: string; title?: string; errors?: Record<string, string[]> }
    | null;

  if (body?.errors) {
    return Object.values(body.errors).flat().join(' ');
  }
  return body?.detail ?? body?.title ?? `The server answered ${error.status}.`;
}