import { apiClient } from './client';
import type { AppSettings, LogEntry, StatItem, WorkerStatus, AnalyticsItem } from '../types';

export const runService = {
  getStatus: () => apiClient.get<WorkerStatus>('/Run/status'),
  start: () => apiClient.post<WorkerStatus>('/Run/start'),
  stop: () => apiClient.post<WorkerStatus>('/Run/stop'),
};

export const dashboardService = {
  getStats: () => apiClient.get<StatItem[]>('/Dashboard/stats'),
  getLogs: (limit: number = 50, marketplace?: string) =>
    apiClient.get<LogEntry[]>(`/Dashboard/logs?limit=${limit}${marketplace ? `&marketplace=${marketplace}` : ''}`),
  // Новый метод
  getAnalytics: () => apiClient.get<AnalyticsItem[]>('/Dashboard/analytics'),
};

export const settingsService = {
  get: () => apiClient.get<AppSettings>('/Settings'),
  update: (settings: AppSettings) => apiClient.post('/Settings', settings),
};

export const authService = {
  login: (password: string) => apiClient.post('/Auth/login', { password }),
  logout: () => apiClient.post('/Auth/logout'),
  check: () => apiClient.get('/Auth/check'),
  // Новый метод
  changePassword: (oldPassword: string, newPassword: string) =>
    apiClient.post('/Auth/change-password', { oldPassword, newPassword }),
};