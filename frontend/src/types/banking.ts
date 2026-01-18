// Bank Integration Types

export enum BankConnectionStatus {
  Pending = 0,
  Active = 1,
  Expired = 2,
  Revoked = 3,
  Error = 4,
}

export enum BankTransactionMatchStatus {
  Unmatched = 0,
  MatchedToInvoice = 1,
  ManuallyBooked = 2,
  Ignored = 3,
}

export interface BankConnection {
  id: string;
  provider: string;
  status: BankConnectionStatus;
  bankName: string | null;
  ibanMasked: string | null;
  lastSyncedAt: string | null;
  expiresAt: string | null;
  createdAt: string;
}

export interface BankTransaction {
  id: string;
  bankConnectionId: string;
  bankName: string | null;
  externalId: string;
  bookingDate: string;
  valueDate: string | null;
  amount: number;
  currency: string;
  counterpartyName: string | null;
  counterpartyIban: string | null;
  description: string | null;
  matchedStatus: BankTransactionMatchStatus;
  matchedInvoiceId: string | null;
  invoiceNumber: string | null;
  matchedAt: string | null;
}

export interface BankConnectionInitiateResponse {
  connectionId: string;
  consentUrl: string;
}

export interface BankSyncResponse {
  transactionsImported: number;
  transactionsUpdated: number;
  syncedAt: string;
}
