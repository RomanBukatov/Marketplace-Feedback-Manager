export interface LogEntry {
  id: number;
  marketplace: string;
  shopId: string;
  reviewId: string;
  reviewText: string;
  rating: number;
  generatedResponse: string;
  processedAt: string;
  isAutoReplied: boolean;
}

export interface StatItem {
  marketplace: string;
  count: number;
}

export interface WorkerStatus {
  isRunning: boolean;
  message?: string;
}

export interface OzonAccount {
  ClientId: string;
  ApiKey: string;
}

export interface WbAccount {
  Token: string;
}

export interface ApiKeys {
  WildberriesAccounts: WbAccount[];
  OpenAI: string;
  OzonAccounts: OzonAccount[];
}

export interface WorkerSettings {
  CheckIntervalSeconds: number;
  MinRating: number;
  SystemPrompt: string;
}

export interface AppSettings {
  ApiKeys: ApiKeys;
  WorkerSettings: WorkerSettings;
}

export interface AnalyticsItem {
  date: string;
  wildberries: number;
  ozon: number;
}