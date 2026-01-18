'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { AppShell } from '@/components/AppShell';
import { contactsApi, invoicesApi, Contact, SalesInvoiceLine } from '@/lib/api';
import styles from './new.module.css';

export default function NewInvoicePage() {
  const router = useRouter();
  const [contacts, setContacts] = useState<Contact[]>([]);
  const [loading, setLoading] = useState(false);

  const [formData, setFormData] = useState({
    contactId: '',
    issueDate: new Date().toISOString().split('T')[0],
    dueDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
    currency: 'EUR',
    notes: '',
  });

  const [lines, setLines] = useState<SalesInvoiceLine[]>([
    { description: '', quantity: 1, unitPrice: 0, vatRate: 21 },
  ]);

  useEffect(() => {
    fetchContacts();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const fetchContacts = async () => {
    try {
      const response = await contactsApi.getAll(1, 100);
      setContacts(response.items);
    } catch (err) {
      console.error('Failed to fetch contacts:', err);
    }
  };

  const addLine = () => {
    setLines([...lines, { description: '', quantity: 1, unitPrice: 0, vatRate: 21 }]);
  };

  const removeLine = (index: number) => {
    if (lines.length > 1) {
      setLines(lines.filter((_, i) => i !== index));
    }
  };

  const updateLine = (index: number, field: keyof SalesInvoiceLine, value: string | number) => {
    const newLines = [...lines];
    newLines[index] = { ...newLines[index], [field]: value };
    setLines(newLines);
  };

  const calculateLineTotal = (line: SalesInvoiceLine) => {
    const subtotal = line.quantity * line.unitPrice;
    const vat = subtotal * (line.vatRate / 100);
    return subtotal + vat;
  };

  const calculateTotals = () => {
    const subtotal = lines.reduce((sum, line) => sum + line.quantity * line.unitPrice, 0);
    const vatTotal = lines.reduce(
      (sum, line) => sum + line.quantity * line.unitPrice * (line.vatRate / 100),
      0
    );
    const total = subtotal + vatTotal;
    return { subtotal, vatTotal, total };
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);

    try {
      const totals = calculateTotals();

      const payload = {
        contactId: formData.contactId,
        issueDate: formData.issueDate,
        dueDate: formData.dueDate,
        currency: formData.currency,
        notes: formData.notes,
        status: 0,
        subtotal: totals.subtotal,
        vatTotal: totals.vatTotal,
        total: totals.total,
        lines: lines.map((line) => ({
          description: line.description,
          quantity: line.quantity,
          unitPrice: line.unitPrice,
          vatRate: line.vatRate,
        })),
      };

      await invoicesApi.create(payload);
      router.push('/invoices');
    } catch (err: any) {
      alert(err.message || 'Error creating invoice');
    } finally {
      setLoading(false);
    }
  };

  const totals = calculateTotals();

  return (
    <ProtectedRoute>
      <AppShell>
        <div className={styles.container}>
          <div className={styles.header}>
            <h1>Create New Invoice</h1>
            <button type="button" onClick={() => router.push('/invoices')} className={styles.backBtn}>
              Cancel
            </button>
          </div>

          <form onSubmit={handleSubmit} className={styles.form}>
            <div className={styles.section}>
              <h2>Invoice Details</h2>
              <div className={styles.grid}>
                <div className={styles.formGroup}>
                  <label>Customer *</label>
                  <select value={formData.contactId} onChange={(e) => setFormData({ ...formData, contactId: e.target.value })} required>
                    <option value="">Select customer...</option>
                    {contacts.map((contact) => (
                      <option key={contact.id} value={contact.id}>{contact.displayName}</option>
                    ))}
                  </select>
                </div>

                <div className={styles.formGroup}>
                  <label>Currency *</label>
                  <select value={formData.currency} onChange={(e) => setFormData({ ...formData, currency: e.target.value })} required>
                    <option value="EUR">EUR</option>
                    <option value="USD">USD</option>
                    <option value="GBP">GBP</option>
                  </select>
                </div>

                <div className={styles.formGroup}>
                  <label>Issue Date *</label>
                  <input type="date" value={formData.issueDate} onChange={(e) => setFormData({ ...formData, issueDate: e.target.value })} required />
                </div>

                <div className={styles.formGroup}>
                  <label>Due Date *</label>
                  <input type="date" value={formData.dueDate} onChange={(e) => setFormData({ ...formData, dueDate: e.target.value })} required />
                </div>
              </div>

              <div className={styles.formGroup}>
                <label>Notes</label>
                <textarea value={formData.notes} onChange={(e) => setFormData({ ...formData, notes: e.target.value })} rows={3} placeholder="Optional notes..." />
              </div>
            </div>

            <div className={styles.section}>
              <div className={styles.linesHeader}>
                <h2>Invoice Lines</h2>
                <button type="button" onClick={addLine} className={styles.addLineBtn}>+ Add Line</button>
              </div>

              <div className={styles.linesTable}>
                <table>
                  <thead>
                    <tr>
                      <th>Description</th>
                      <th>Quantity</th>
                      <th>Unit Price</th>
                      <th>VAT %</th>
                      <th>Line Total</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    {lines.map((line, index) => (
                      <tr key={index}>
                        <td><input type="text" value={line.description} onChange={(e) => updateLine(index, 'description', e.target.value)} placeholder="Description..." required /></td>
                        <td><input type="number" value={line.quantity} onChange={(e) => updateLine(index, 'quantity', parseFloat(e.target.value))} min="0" step="0.01" required /></td>
                        <td><input type="number" value={line.unitPrice} onChange={(e) => updateLine(index, 'unitPrice', parseFloat(e.target.value))} min="0" step="0.01" required /></td>
                        <td>
                          <select value={line.vatRate} onChange={(e) => updateLine(index, 'vatRate', parseFloat(e.target.value))}>
                            <option value={0}>0%</option>
                            <option value={9}>9%</option>
                            <option value={21}>21%</option>
                          </select>
                        </td>
                        <td>€ {calculateLineTotal(line).toFixed(2)}</td>
                        <td>{lines.length > 1 && (<button type="button" onClick={() => removeLine(index)} className={styles.removeBtn}></button>)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>

            <div className={styles.totalsSection}>
              <div className={styles.totalsGrid}>
                <div className={styles.totalRow}><span>Subtotal:</span><span>€ {totals.subtotal.toFixed(2)}</span></div>
                <div className={styles.totalRow}><span>VAT:</span><span>€ {totals.vatTotal.toFixed(2)}</span></div>
                <div className={`${styles.totalRow} ${styles.grandTotal}`}><span>Total:</span><span>€ {totals.total.toFixed(2)}</span></div>
              </div>
            </div>

            <div className={styles.actions}>
              <button type="button" onClick={() => router.push('/invoices')} className={styles.cancelBtn}>Cancel</button>
              <button type="submit" className={styles.submitBtn} disabled={loading}>{loading ? 'Creating...' : 'Create Invoice'}</button>
            </div>
          </form>
        </div>
      </AppShell>
    </ProtectedRoute>
  );
}
