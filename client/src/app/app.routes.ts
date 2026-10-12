import { Routes } from '@angular/router';
import { authGuard } from '@features/auth/auth.guard';
import { guestGuard } from '@features/auth/guest.guard';

export const routes: Routes = [
  {
    path: 'login',
    title: 'Log in · UP',
    canMatch: [guestGuard],
    loadComponent: () => import('@features/auth/login-page/login-page').then((m) => m.LoginPage),
  },
  {
    path: '',
    canMatch: [authGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        title: 'UP',
        loadComponent: () => import('@features/home/home-page').then((m) => m.HomePage),
      },
    ],
  },
  {
    path: '**',
    title: 'Page not found · UP',
    loadComponent: () => import('@features/not-found/not-found-page').then((m) => m.NotFoundPage),
  },
];
