// Invoice Templates
export interface InvoiceTemplate {
  id: string;
  name: string;
  isDefault: boolean;
  htmlTemplate: string;
  cssTemplate: string;
  settingsJson?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateInvoiceTemplate {
  name: string;
  isDefault: boolean;
  htmlTemplate: string;
  cssTemplate: string;
  settingsJson?: string;
}

export interface UpdateInvoiceTemplate {
  name?: string;
  isDefault?: boolean;
  htmlTemplate?: string;
  cssTemplate?: string;
  settingsJson?: string;
}

// Tenant Branding
export interface TenantBranding {
  id: string;
  tenantId: string;
  logoUrl?: string;
  primaryColor?: string;
  secondaryColor?: string;
  fontFamily?: string;
  footerText?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface UpdateTenantBranding {
  logoUrl?: string;
  primaryColor?: string;
  secondaryColor?: string;
  fontFamily?: string;
  footerText?: string;
}

// Sales Invoices
export enum InvoiceStatus {
  Draft = 0,
  Sent = 1,
  Posted = 2,
  Paid = 3
}

export interface SalesInvoiceLine {
  id: string;
  lineNumber: number;
  description: string;
  quantity: number;
  unitPrice: number;
  vatRate: number;
  lineSubtotal: number;
  lineVatAmount: number;
  lineTotal: number;
  accountId?: string;
}

export interface SalesInvoice {
  id: string;
  invoiceNumber: string;
  status: InvoiceStatus;
  issueDate: string;
  dueDate: string;
  contactId: string;
  contactName: string;
  currency: string;
  subtotal: number;
  vatTotal: number;
  total: number;
  pdfFileId?: string;
  templateId?: string;
  notes?: string;
  journalEntryId?: string;
  lines: SalesInvoiceLine[];
  createdAt: string;
  updatedAt?: string;
}

export interface CreateSalesInvoiceLine {
  description: string;
  quantity: number;
  unitPrice: number;
  vatRate: number;
  accountId?: string;
}

export interface CreateSalesInvoice {
  invoiceNumber: string;
  issueDate: string;
  dueDate: string;
  contactId: string;
  currency: string;
  templateId?: string;
  notes?: string;
  lines: CreateSalesInvoiceLine[];
}

export interface UpdateSalesInvoice {
  invoiceNumber?: string;
  issueDate?: string;
  dueDate?: string;
  contactId?: string;
  currency?: string;
  templateId?: string;
  notes?: string;
  lines?: CreateSalesInvoiceLine[];
}
