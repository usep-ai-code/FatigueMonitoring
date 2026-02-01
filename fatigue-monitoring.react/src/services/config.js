// API Configuration
// In production, this should come from environment variables

const getApiBaseUrl = () => {
  // Check for Vite environment variables
  if (import.meta.env.VITE_API_URL) {
    return import.meta.env.VITE_API_URL;
  }
  
  // Default to localhost for development
  if (import.meta.env.DEV) {
    return 'https://localhost:7001';
  }
  
  // Production: assume same origin or configure via env
  return window.location.origin;
};

export const API_CONFIG = {
  BASE_URL: getApiBaseUrl(),
  ENDPOINTS: {
    DASHBOARD: '/api/dashboard',
    DASHBOARD_STREAM: '/api/dashboard/stream',
    CONNECTIONS: '/api/dashboard/connections'
  }
};

export const getApiUrl = (endpoint) => {
  return `${API_CONFIG.BASE_URL}${endpoint}`;
};
