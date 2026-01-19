/**
 * Central API Client for Boekhouding SaaS
 * 
 * Features:
 * - Auto-injects JWT token
 * - Auto-injects X-Tenant-Id header
 * - Centralized error handling
 * - 401 → redirect to login
 * - TypeScript typed responses
 */

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';

// ============================================================================
// Types
// ============================================================================

export interface ApiError {
  message: string;
  statusCode: number;
  errors?: Record<string, string[]>;
}

interface ApiClientOptions {
  token?: string;
  tenantId?: string;
  skipAuth?: boolean;
}

// ============================================================================
// Core Client
// ============================================================================

class ApiClient {
  private baseUrl: string;

  constructor(baseUrl: string = API_BASE_URL) {
    this.baseUrl = baseUrl;
  }

  /**
   * Build headers with auth automatically
   */
  private getHeaders(options: ApiClientOptions = {}): HeadersInit {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    };

    // Get token from options or localStorage
    const token = options.token || (typeof window !== 'undefined' ? localStorage.getItem('token') : null);
    const tenantId = options.tenantId || (typeof window !== 'undefined' ? localStorage.getItem('tenantId') : null);

    if (token && !options.skipAuth) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    if (tenantId && !options.skipAuth) {
      headers['X-Tenant-Id'] = tenantId;
    }

    return headers;
  }

  /**
   * Handle API errors
   */
  private async handleResponse<T>(response: Response): Promise<T> {
    // Handle 401 - redirect to login
    if (response.status === 401) {
      if (typeof window !== 'undefined') {
        localStorage.clear();
        window.location.href = '/login';
      }
      throw new Error('Unauthorized');
    }

    // Handle other errors
    if (!response.ok) {
      let errorMessage = `API Error: ${response.status}`;
      
      try {
        const errorData = await response.json();
        errorMessage = errorData.message || errorData.error || errorMessage;
      } catch {
        // Response body is not JSON
      }

      throw new Error(errorMessage);
    }

    // Handle 204 No Content
    if (response.status === 204) {
      return undefined as T;
    }

    // Parse JSON response
    return response.json();
  }

  /**
   * GET request
   */
  async get<T>(endpoint: string, options: ApiClientOptions = {}): Promise<T> {
    const response = await fetch(`${this.baseUrl}${endpoint}`, {
      method: 'GET',
      headers: this.getHeaders(options),
    });

    return this.handleResponse<T>(response);
  }

  /**
   * POST request
   */
  async post<T>(endpoint: string, body?: any, options: ApiClientOptions = {}): Promise<T> {
    const response = await fetch(`${this.baseUrl}${endpoint}`, {
      method: 'POST',
      headers: this.getHeaders(options),
      body: body ? JSON.stringify(body) : undefined,
    });

    return this.handleResponse<T>(response);
  }

  /**
   * PUT request
   */
  async put<T>(endpoint: string, body?: any, options: ApiClientOptions = {}): Promise<T> {
    const response = await fetch(`${this.baseUrl}${endpoint}`, {
      method: 'PUT',
      headers: this.getHeaders(options),
      body: body ? JSON.stringify(body) : undefined,
    });

    return this.handleResponse<T>(response);
  }

  /**
   * DELETE request
   */
  async delete<T>(endpoint: string, options: ApiClientOptions = {}): Promise<T> {
    const response = await fetch(`${this.baseUrl}${endpoint}`, {
      method: 'DELETE',
      headers: this.getHeaders(options),
    });

    return this.handleResponse<T>(response);
  }

  /**
   * Download file (e.g., PDF)
   */
  async downloadFile(endpoint: string, filename: string, options: ApiClientOptions = {}): Promise<void> {
    const response = await fetch(`${this.baseUrl}${endpoint}`, {
      method: 'GET',
      headers: this.getHeaders(options),
    });

    if (!response.ok) {
      throw new Error(`Download failed: ${response.status}`);
    }

    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);
  }
}

// ============================================================================
// API Instance
// ============================================================================

export const api = new ApiClient();

// ============================================================================
// Typed API Methods
// ============================================================================

// Auth
export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  userId: string;
  email: string;
  role: string;
}

export const authApi = {
  login: (data: LoginRequest) => 
    api.post<LoginResponse>('/api/auth/login', data, { skipAuth: true }),
};

// Tenants
export interface Tenant {
  id: string;
  name: string;
  role: string;
  kvK?: string;
  vatNumber?: string;
}

export const tenantsApi = {
  getMy: () => api.get<Tenant>('/api/tenants/my'),
  getById: (id: string) => api.get<Tenant>(`/api/tenants/${id}`),
  create: (data: { name: string; kvK?: string; vatNumber?: string }) =>
    api.post<Tenant>('/api/tenants', data),
};

// Contacts
export interface Contact {
  id: string;
  type: number; // 0=Customer, 1=Supplier, 2=Both
  displayName: string;
  email?: string;
  phone?: string;
  addressLine1?: string;
  addressLine2?: string;
  postalCode?: string;
  city?: string;
  country?: string;
  vatNumber?: string;
  kvK?: string;
  isActive: boolean;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export const contactsApi = {
  getAll: (page = 1, pageSize = 25) =>
    api.get<PaginatedResponse<Contact>>(`/api/contacts?page=${page}&pageSize=${pageSize}`),
  getById: (id: string) => api.get<Contact>(`/api/contacts/${id}`),
  create: (data: Partial<Contact>) => api.post<Contact>('/api/contacts', data),
  update: (id: string, data: Partial<Contact>) => api.put<Contact>(`/api/contacts/${id}`, data),
  delete: (id: string) => api.delete(`/api/contacts/${id}`),
};

// Sales Invoices
export interface SalesInvoiceLine {
  description: string;
  quantity: number;
  unitPrice: number;
  vatRate: number;
  accountId?: string;
}

export interface PaymentTransaction {
  transactionId: string;
  bookingDate: string;
  amount: number;
  currency: string;
  counterpartyName?: string;
  description?: string;
  matchedAt: string;
}

export interface SalesInvoice {
  id: string;
  invoiceNumber: string;
  status: number; // 0=Draft, 1=Sent, 2=Posted, 3=Paid
  issueDate: string;
  dueDate: string;
  contactId: string;
  contactName: string;
  currency: string;
  subtotal: number;
  vatTotal: number;
  total: number;
  notes?: string;
  lines: SalesInvoiceLine[];
  openAmount?: number;
  journalEntryId?: string;
  payments?: PaymentTransaction[];
}

export const invoicesApi = {
  getAll: () => api.get<SalesInvoice[]>('/api/salesinvoices'),
  getById: (id: string) => api.get<SalesInvoice>(`/api/salesinvoices/${id}`),
  create: (data: any) => api.post<SalesInvoice>('/api/salesinvoices', data),
  update: (id: string, data: any) => api.put<SalesInvoice>(`/api/salesinvoices/${id}`, data),
  delete: (id: string) => api.delete(`/api/salesinvoices/${id}`),
  post: (id: string) => api.post<SalesInvoice>(`/api/salesinvoices/${id}/post`),
  renderPdf: (id: string, filename: string) =>
    api.downloadFile(`/api/salesinvoices/${id}/render-pdf`, filename),
};

// Bank
export interface BankConnection {
  id: string;
  provider: string;
  bankName: string;
  ibanMasked: string;
  status: number; // 0=Pending, 1=Active, 2=Expired
  lastSyncedAt?: string;
}

export interface BankTransaction {
  id: string;
  bankConnectionId: string;
  bookingDate: string;
  amount: number;
  currency: string;
  counterpartyName?: string;
  counterpartyIban?: string;
  description?: string;
  matchedStatus: number; // 0=Unmatched, 1=Matched, 2=Manual, 3=Ignored
  matchedInvoiceId?: string;
  invoiceNumber?: string;
  journalEntryId?: string;
}

export const bankApi = {
  getConnections: () => api.get<BankConnection[]>('/api/bank/connections'),
  connect: (provider: string) =>
    api.post<{ authorizationUrl: string }>('/api/bank/connect', { provider }),
  sync: (connectionId: string, from: string, to: string) =>
    api.post<{ transactionsImported: number; transactionsUpdated: number }>(
      `/api/bank/connections/${connectionId}/sync`,
      { from, to }
    ),
  deleteConnection: (id: string) => api.delete(`/api/bank/connections/${id}`),
  getTransactions: (connectionId?: string) => {
    const endpoint = connectionId
      ? `/api/bank/transactions?connectionId=${connectionId}`
      : '/api/bank/transactions';
    return api.get<BankTransaction[]>(endpoint);
  },
  matchTransaction: (transactionId: string, invoiceId: string) =>
    api.post<BankTransaction>(`/api/bank/transactions/${transactionId}/match`, { invoiceId }),
};

// Reports
export interface VatReport {
  fromDate: string;
  toDate: string;
  vatRates: Array<{
    vatRate: number;
    revenue: number;
    vatAmount: number;
    lineCount: number;
  }>;
  totalRevenue: number;
  totalVat: number;
  totalIncludingVat: number;
  invoiceCount: number;
}

export const reportsApi = {
  getVatReport: (from: string, to: string) =>
    api.get<VatReport>(`/api/reports/vat?from=${from}&to=${to}`),
};

// Audit Logs
export interface AuditLog {
  id: string;
  timestamp: string;
  action: string;
  entityType: string;
  entityId: string;
  actorUserId: string;
  actor: {
    email: string;
  };
  diffJson?: string | null;
  ipAddress?: string;
  userAgent?: string;
}

export const auditApi = {
  getLogs: (skip = 0, take = 50, action?: string) => {
    let endpoint = `/api/auditlogs?skip=${skip}&take=${take}`;
    if (action) endpoint += `&action=${action}`;
    return api.get<AuditLog[]>(endpoint);
  },
};

// Settings - Branding
export interface TenantBranding {
  id: string;
  tenantId: string;
  logoUrl?: string;
  primaryColor?: string;
  secondaryColor?: string;
  fontFamily?: string;
  footerText?: string;
}

export const brandingApi = {
  get: () => api.get<TenantBranding>('/api/tenantbranding'),
  update: (data: Partial<TenantBranding>) => api.put<TenantBranding>('/api/tenantbranding', data),
};

// Settings - Templates
export interface InvoiceTemplate {
  id: string;
  name: string;
  isDefault: boolean;
  htmlTemplate: string;
  cssTemplate: string;
  settingsJson?: string;
}

export const templatesApi = {
  getAll: () => api.get<InvoiceTemplate[]>('/api/invoicetemplates'),
  getById: (id: string) => api.get<InvoiceTemplate>(`/api/invoicetemplates/${id}`),
  create: (data: any) => api.post<InvoiceTemplate>('/api/invoicetemplates', data),
  update: (id: string, data: any) => api.put<InvoiceTemplate>(`/api/invoicetemplates/${id}`, data),
  delete: (id: string) => api.delete(`/api/invoicetemplates/${id}`),
};

// ============================================================================
// Export Default
// ============================================================================

export default api;
