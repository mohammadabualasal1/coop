import { HttpErrorResponse } from '@angular/common/http';

export function extractErrorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (typeof error.error === 'string' && error.error.trim().length > 0) {
      return error.error;
    }

    if (error.error && typeof error.error === 'object') {
      const body = error.error as { message?: unknown; errors?: Record<string, unknown> };

      if (typeof body.message === 'string' && body.message.trim().length > 0) {
        return body.message;
      }

      if (body.errors && typeof body.errors === 'object') {
        const firstValue = Object.values(body.errors)[0];

        if (Array.isArray(firstValue) && typeof firstValue[0] === 'string') {
          return firstValue[0];
        }

        if (typeof firstValue === 'string') {
          return firstValue;
        }
      }
    }

    if (error.status === 0) {
      return 'تعذّر الاتصال بالخادم. تأكد من اتصالك بالإنترنت.';
    }
  }

  return 'حدث خطأ غير متوقع. حاول مرة أخرى.';
}
