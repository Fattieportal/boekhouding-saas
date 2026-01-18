'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { AppShell } from '@/components/AppShell';
import { invoicesApi, SalesInvoice } from '@/lib/api';
import styles from './invoices.module.css';

const statusLabels = ['Draft', 'Sent', 'Posted', 'Paid'];
const statusColors = ['#999', '#0066cc', '#ff9800', '#4caf50'];

export default function InvoicesPage() {
  const [invoices, setInvoices] = useState<SalesInvoice[]>([]);
  const [filteredInvoices, setFilteredInvoices] = useState<SalesInvoice[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<number | 'all'>('all');
  const router = useRouter();

  useEffect(() => {
    fetchInvoices();
  }, []);

  useEffect(() => {
    if (statusFilter === 'all') {
      setFilteredInvoices(invoices);
    } else {
      setFilteredInvoices(invoices.filter(inv => inv.status === statusFilter));
    }
  }, [invoices, statusFilter]);

  const fetchInvoices = async () => {
    try {
      setLoading(true);
      const data = await invoicesApi.getAll();
      setInvoices(data);
      setError(null);
    } catch (err: any) {
      setError(err.message || 'Failed to fetch invoices');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this invoice?')) return;

    try {
      await invoicesApi.delete(id);
      fetchInvoices();
    } catch (err: any) {
      alert(err.message || 'Failed to delete invoice');
    }
  };

  const handlePost = async (id: string) => {
    if (!confirm('Are you sure you want to post this invoice? This action cannot be undone.')) return;

    try {
      await invoicesApi.post(id);
      fetchInvoices();
      alert('Invoice posted successfully!');
    } catch (err: any) {
      alert(err.message || 'Failed to post invoice');
    }
  };

  const handleDownloadPdf = async (id: string, invoiceNumber: string) => {
    try {
      await invoicesApi.renderPdf(id, `invoice_${invoiceNumber}.pdf`);
    } catch (err: any) {
      alert(err.message || 'Failed to download PDF');
    }
  };

  return (
    <ProtectedRoute>
      <AppShell>
        <div className={styles.container}>
          <div className={styles.header}>
            <h1>Sales Invoices</h1>
            <div className={styles.headerActions}>
              <div className={styles.filterGroup}>
                <label htmlFor="status-filter">Status:</label>
                <select
                  id="status-filter"
                  value={statusFilter}
                  onChange={(e) => setStatusFilter(e.target.value === 'all' ? 'all' : parseInt(e.target.value))}
                  className={styles.filterSelect}
                >
                  <option value="all">All Statuses</option>
                  <option value={0}>Draft</option>
                  <option value={1}>Sent</option>
                  <option value={2}>Posted</option>
                  <option value={3}>Paid</option>
                </select>
              </div>
              <button
                className={styles.createButton}
                onClick={() => router.push('/invoices/new')}
              >
                + Create Invoice
              </button>
            </div>
          </div>

          {loading && <div className={styles.loading}>Loading invoices...</div>}
          {error && <div className={styles.error}>{error}</div>}

          {!loading && !error && (
            <div className={styles.tableContainer}>
              <table className={styles.table}>
                <thead>
                  <tr>
                    <th>Invoice #</th>
                    <th>Contact</th>
                    <th>Issue Date</th>
                    <th>Due Date</th>
                    <th>Amount</th>
                    <th>Status</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredInvoices.length === 0 ? (
                    <tr>
                      <td colSpan={7} className={styles.noData}>
                        No invoices found. Create your first invoice!
                      </td>
                    </tr>
                  ) : (
                    filteredInvoices.map((invoice) => (
                      <tr key={invoice.id}>
                        <td>
                          <button
                            className={styles.linkButton}
                            onClick={() => router.push(`/invoices/${invoice.id}`)}
                          >
                            {invoice.invoiceNumber}
                          </button>
                        </td>
                        <td>{invoice.contactName || '-'}</td>
                        <td>{new Date(invoice.issueDate).toLocaleDateString()}</td>
                        <td>{new Date(invoice.dueDate).toLocaleDateString()}</td>
                        <td>
                          {invoice.currency} {invoice.total.toFixed(2)}
                        </td>
                        <td>
                          <span
                            className={styles.statusBadge}
                            style={{ backgroundColor: statusColors[invoice.status] }}
                          >
                            {statusLabels[invoice.status]}
                          </span>
                        </td>
                        <td>
                          <div className={styles.actions}>
                            <button
                              className={styles.actionButton}
                              onClick={() => handleDownloadPdf(invoice.id, invoice.invoiceNumber)}
                              title="Download PDF"
                            >
                              📄
                            </button>
                            {invoice.status === 0 && (
                              <>
                                <button
                                  className={styles.actionButton}
                                  onClick={() => handlePost(invoice.id)}
                                  title="Post Invoice"
                                >
                                  ✅
                                </button>
                                <button
                                  className={styles.actionButton}
                                  onClick={() => router.push(`/invoices/${invoice.id}`)}
                                  title="Edit"
                                >
                                  ✏️
                                </button>
                                <button
                                  className={`${styles.actionButton} ${styles.deleteButton}`}
                                  onClick={() => handleDelete(invoice.id)}
                                  title="Delete"
                                >
                                  🗑️
                                </button>
                              </>
                            )}
                          </div>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </AppShell>
    </ProtectedRoute>
  );
}
