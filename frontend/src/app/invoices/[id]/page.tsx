'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { AppShell } from '@/components/AppShell';
import { invoicesApi, SalesInvoice } from '@/lib/api';
import styles from './detail.module.css';

const statusLabels = ['Draft', 'Sent', 'Posted', 'Paid'];

const formatCurrency = (amount: number, currency: string = 'EUR'): string => {
  return new Intl.NumberFormat('nl-NL', { style: 'currency', currency }).format(amount);
};

const formatDate = (dateString: string): string => {
  return new Date(dateString).toLocaleDateString('nl-NL', { 
    year: 'numeric', 
    month: 'short', 
    day: 'numeric' 
  });
};

export default function InvoiceDetailPage() {
  const params = useParams();
  const router = useRouter();
  const [invoice, setInvoice] = useState<SalesInvoice | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (params.id) { fetchInvoice(); }
  }, [params.id]);

  const fetchInvoice = async () => {
    try {
      setLoading(true);
      const data = await invoicesApi.getById(params.id as string);
      setInvoice(data);
      setError(null);
    } catch (err: any) {
      setError(err.message || 'Failed to fetch invoice');
    } finally {
      setLoading(false);
    }
  };

  const handlePost = async () => {
    if (!invoice || !confirm('Post this invoice?')) return;
    try { await invoicesApi.post(invoice.id); fetchInvoice(); alert('Posted!'); } catch (err: any) { alert(err.message); }
  };

  const handleDownloadPdf = async () => {
    if (!invoice) return;
    try { await invoicesApi.renderPdf(invoice.id, `invoice_${invoice.invoiceNumber}.pdf`); } catch (err: any) { alert(err.message); }
  };

  const handleDelete = async () => {
    if (!invoice || !confirm('Delete?')) return;
    try { await invoicesApi.delete(invoice.id); router.push('/invoices'); } catch (err: any) { alert(err.message); }
  };

  if (loading) return (<ProtectedRoute><AppShell><div>Loading...</div></AppShell></ProtectedRoute>);
  if (error || !invoice) return (<ProtectedRoute><AppShell><div>{error}</div></AppShell></ProtectedRoute>);

  return (
    <ProtectedRoute><AppShell><div className={styles.container}>
      <div className={styles.header}>
        <div><h1>Invoice {invoice.invoiceNumber}</h1><span>{statusLabels[invoice.status]}</span></div>
        <div><button onClick={() => router.push('/invoices')}>Back</button><button onClick={handleDownloadPdf}>PDF</button>{invoice.status === 0 && <><button onClick={handlePost}>Post</button><button onClick={handleDelete}>Delete</button></>}</div>
      </div>
      <div><h2>Details</h2><p>Customer: {invoice.contactName}</p><p>Issue: {new Date(invoice.issueDate).toLocaleDateString()}</p><p>Due: {new Date(invoice.dueDate).toLocaleDateString()}</p></div>
      <div><h2>Lines</h2><table><thead><tr><th>Description</th><th>Qty</th><th>Price</th><th>VAT</th><th>Total</th></tr></thead><tbody>{invoice.lines.map((l, i) => <tr key={i}><td>{l.description}</td><td>{l.quantity}</td><td>{l.unitPrice}</td><td>{l.vatRate}%</td><td>{(l.quantity * l.unitPrice * (1 + l.vatRate / 100)).toFixed(2)}</td></tr>)}</tbody></table></div>
      <div><p>Subtotal: {invoice.subtotal.toFixed(2)}</p><p>VAT: {invoice.vatTotal.toFixed(2)}</p><p>Total: {invoice.total.toFixed(2)}</p>{invoice.openAmount !== undefined && invoice.openAmount > 0 && <p><strong>Open Amount: {formatCurrency(invoice.openAmount, invoice.currency)}</strong></p>}</div>
      
      {/* Payments Section */}
      {invoice.payments && invoice.payments.length > 0 && (
        <div className={styles.paymentsSection}>
          <h2>Payments</h2>
          <table className={styles.paymentsTable}>
            <thead>
              <tr>
                <th>Date</th>
                <th>Amount</th>
                <th>Counterparty</th>
                <th>Description</th>
                <th>Matched At</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {invoice.payments.map((payment) => (
                <tr key={payment.transactionId}>
                  <td>{formatDate(payment.bookingDate)}</td>
                  <td>{formatCurrency(payment.amount, payment.currency)}</td>
                  <td>{payment.counterpartyName || '-'}</td>
                  <td>{payment.description || '-'}</td>
                  <td>{formatDate(payment.matchedAt)}</td>
                  <td>
                    <button 
                      onClick={() => router.push('/banking/transactions')}
                      className={styles.linkButton}
                    >
                      View Transaction
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Journal Entry Link */}
      {invoice.journalEntryId && (
        <div className={styles.journalSection}>
          <button 
            onClick={() => router.push(`/accounting/journal-entries`)}
            className={styles.journalButton}
          >
            📊 View Journal Entry
          </button>
        </div>
      )}
    </div></AppShell></ProtectedRoute>
  );
}
