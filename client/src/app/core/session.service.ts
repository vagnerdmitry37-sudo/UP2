import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { API_BASE_URL } from '@core/api-base-url';
import { firstValueFrom } from 'rxjs';

interface AccessTokenResponse {
  accessToken: string;
  expiresAt: string;
}

const rejectedCredentialStatuses = [400, 401];

@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly http = inject(HttpClient);
  private readonly authUrl = `${inject(API_BASE_URL)}/auth`;
  private readonly token = signal<string | null>(null);

  readonly accessToken = this.token.asReadonly();
  readonly isAuthenticated = computed(() => this.token() !== null);

  async login(email: string, password: string): Promise<boolean> {
    try {
      await this.requestToken('login', { email, password });
      return true;
    } catch (error) {
      if (error instanceof HttpErrorResponse && rejectedCredentialStatuses.includes(error.status)) {
        return false;
      }
      throw error;
    }
  }

  async restore(): Promise<void> {
    try {
      await this.requestToken('refresh', {});
    } catch {
      this.token.set(null);
    }
  }

  async logout(): Promise<void> {
    try {
      await firstValueFrom(this.http.post<void>(`${this.authUrl}/logout`, {}));
    } finally {
      this.token.set(null);
    }
  }

  private async requestToken(endpoint: 'login' | 'refresh', body: object): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<AccessTokenResponse>(`${this.authUrl}/${endpoint}`, body),
    );
    this.token.set(response.accessToken);
  }
}
