import axios from 'axios';

// Адрес бэкенда (если порт другой - поменяй тут)
const API_URL = 'http://localhost:5035/api';

export const apiClient = axios.create({
  baseURL: API_URL,
  withCredentials: true, // <--- ВОТ ЭТО ОБЯЗАТЕЛЬНО
  headers: {
    'Content-Type': 'application/json',
  },
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response && error.response.status === 401) {
      // Если мы не на странице логина - редирект
      if (window.location.pathname !== '/login') {
        window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  }
);