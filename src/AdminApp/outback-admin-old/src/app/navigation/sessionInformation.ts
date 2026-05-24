export interface SessionInformation {
    isAuthenticated: boolean;
    expiration: number;
    name: string;
    loginUrl: string;
    logoutUrl: string;
  }