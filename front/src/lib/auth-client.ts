import axios, { AxiosInstance } from 'axios';

const baseURL = process.env.NEXT_PUBLIC_API_URL;

/**
 * Dedicated axios instance for authentication requests.
 * Used for login, registration, and token refresh to avoid 
 * side-effects from global apiClient interceptors.
 */
const authClient: AxiosInstance = axios.create({
  baseURL: baseURL || '',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

export default authClient;
