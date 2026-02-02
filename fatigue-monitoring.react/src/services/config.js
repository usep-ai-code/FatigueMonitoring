// API Configuration
// In production, this should come from environment variables

const getApiBaseUrl = () => {
  // Check for Vite environment variables (and make sure it's not empty)
  const envUrl = import.meta.env.VITE_API_URL;
  if (envUrl && envUrl.trim() !== '') {
    return envUrl.trim();
  }
  
  // Default to localhost for development
  if (import.meta.env.DEV) {
    return 'https://localhost:7217';
  }
  
  // Production: use same origin (frontend served by backend)
  return '';  // Empty string = same origin, relative URLs
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
