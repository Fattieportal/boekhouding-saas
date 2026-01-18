'use client';
import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { AppShell } from '@/components/AppShell';
import { bankApi, invoicesApi, BankTransaction, SalesInvoice } from '@/lib/api';
import styles from './transactions.module.css';

export default function BankTransactionsPage() {
  const [transactions, setTransactions] = useState<BankTransaction[]>([]);
  const [invoices, setInvoices] = useState<SalesInvoice[]>([]);
  const [loading, setLoading] = useState(false);
  const [matchFilter, setMatchFilter] = useState<'all' | 'matched' | 'unmatched'>('all');
  const [filteredTransactions, setFilteredTransactions] = useState<BankTransaction[]>([]);
  const [showMatchDialog, setShowMatchDialog] = useState(false);
  const [selectedTransaction, setSelectedTransaction] = useState<BankTransaction | null>(null);
  const router = useRouter();

  useEffect(() => { fetchData(); }, []);

  useEffect(() => {
    if (matchFilter === 'all') {
      setFilteredTransactions(transactions);
    } else if (matchFilter === 'matched') {
      setFilteredTransactions(transactions.filter(t => t.matchedStatus === 1));
    } else {
      setFilteredTransactions(transactions.filter(t => t.matchedStatus === 0));
    }
  }, [transactions, matchFilter]);

  const fetchData = async () => {
    try {
      setLoading(true);
      const [txns, invs] = await Promise.all([bankApi.getTransactions(), invoicesApi.getAll()]);
      setTransactions(txns);
      setInvoices(invs.filter(inv => inv.status === 2));
    } catch (err: any) {
      alert(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleMatch = async (transactionId: string, invoiceId: string) => {
    try {
      await bankApi.matchTransaction(transactionId, invoiceId);
      setShowMatchDialog(false);
      setSelectedTransaction(null);
      fetchData();
    } catch (err: any) {
      alert(err.message);
    }
  };

  const openMatchDialog = (transaction: BankTransaction) => {
    setSelectedTransaction(transaction);
    setShowMatchDialog(true);
  };

  const matchStatus = ['Unmatched', 'Matched', 'Manual', 'Ignored'];

  return (
    <ProtectedRoute><AppShell><div className={styles.container}>
      <div className={styles.header}>
        <h1>Bank Transactions</h1>
        <div className={styles.headerActions}>
          <select value={matchFilter} onChange={(e) => setMatchFilter(e.target.value as any)} className={styles.filterSelect}>
            <option value="all">All Transactions</option>
            <option value="matched">Matched Only</option>
            <option value="unmatched">Unmatched Only</option>
          </select>
          <button onClick={() => router.push('/banking/connections')}>Manage Connections</button>
        </div>
      </div>
      {loading && <div>Loading...</div>}
      {!loading && filteredTransactions.length === 0 && matchFilter === 'all' && <div>No transactions. Sync your bank connection first.</div>}
      {!loading && filteredTransactions.length === 0 && matchFilter !== 'all' && <div>No {matchFilter} transactions found.</div>}
      {!loading && filteredTransactions.length > 0 && (
        <table className={styles.table}>
          <thead><tr><th>Date</th><th>Amount</th><th>Counterparty</th><th>Description</th><th>Status</th><th>Match</th></tr></thead>
          <tbody>{filteredTransactions.map(t => (
            <tr key={t.id}>
              <td>{new Date(t.bookingDate).toLocaleDateString()}</td>
              <td>{t.currency} {t.amount.toFixed(2)}</td>
              <td>{t.counterpartyName || '-'}</td>
              <td>{t.description || '-'}</td>
              <td>{matchStatus[t.matchedStatus]}</td>
              <td>
                {t.matchedStatus === 0 && <button onClick={() => openMatchDialog(t)} className={styles.matchBtn}>Match Invoice</button>}
                {t.matchedStatus === 1 && t.invoiceNumber && t.matchedInvoiceId && (
                  <button 
                    onClick={() => router.push(`/invoices/${t.matchedInvoiceId}`)}
                    className={styles.invoiceLink}
                  >
                    {t.invoiceNumber}
                  </button>
                )}
              </td>
            </tr>
          ))}</tbody>
        </table>
      )}

      {showMatchDialog && selectedTransaction && (
        <div className={styles.modalOverlay} onClick={() => setShowMatchDialog(false)}>
          <div className={styles.modalContent} onClick={(e) => e.stopPropagation()}>
            <h2>Match Transaction</h2>
            <div className={styles.transactionInfo}>
              <p><strong>Amount:</strong> {selectedTransaction.currency} {selectedTransaction.amount.toFixed(2)}</p>
              <p><strong>Date:</strong> {new Date(selectedTransaction.bookingDate).toLocaleDateString()}</p>
              <p><strong>Counterparty:</strong> {selectedTransaction.counterpartyName || '-'}</p>
              <p><strong>Description:</strong> {selectedTransaction.description || '-'}</p>
            </div>
            <label>Select Invoice:</label>
            <div className={styles.invoiceList}>
              {invoices.length === 0 && <p>No posted invoices available for matching.</p>}
              {invoices.map(inv => (
                <div key={inv.id} className={styles.invoiceItem} onClick={() => handleMatch(selectedTransaction.id, inv.id)}>
                  <div>
                    <strong>{inv.invoiceNumber}</strong>
                    <span>{inv.contactName}</span>
                  </div>
                  <div className={styles.invoiceAmount}>{inv.currency} {inv.total.toFixed(2)}</div>
                </div>
              ))}
            </div>
            <button onClick={() => setShowMatchDialog(false)} className={styles.cancelBtn}>Cancel</button>
          </div>
        </div>
      )}
    </div></AppShell></ProtectedRoute>
  );
}
