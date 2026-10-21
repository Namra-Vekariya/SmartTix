export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  phone?: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  user: User;
}

export interface User {
  id: string;
  fullName: string;
  email: string;
  role: 'Customer' | 'Admin';
}
