import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable({ providedIn: 'root' })
export class RoleGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot): boolean {
    const allowedRoles = route.data['roles'] as string[] | undefined;

    if (!allowedRoles || allowedRoles.length === 0) {
      return true;
    }

    const userRole = this.authService.role;

    if (allowedRoles.includes(userRole)) {
      return true;
    }

    // Redirect based on role
    if (userRole === 'Contador') {
      this.router.navigate(['/taxable-expenses']);
    } else {
      this.router.navigate(['/dashboard']);
    }

    return false;
  }
}
