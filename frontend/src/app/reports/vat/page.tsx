'use client';
import { useState } from 'react';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { AppShell } from '@/components/AppShell';
import { reportsApi, VatReport } from '@/lib/api';
import styles from './vat.module.css';

export default function VatReportPage() {
  const [report, setReport] = useState<VatReport | null>(null);
  const [loading, setLoading] = useState(false);
  const [fromDate, setFromDate] = useState(new Date(new Date().getFullYear(), 0, 1).toISOString().split('T')[0]);
  const [toDate, setToDate] = useState(new Date().toISOString().split('T')[0]);

  const handleGenerate = async () => {
    setLoading(true);
    try {
      const data = await reportsApi.getVatReport(fromDate, toDate);
      setReport(data);
    } catch (err: any) {
      alert(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <ProtectedRoute><AppShell><div className={styles.container}>
      <h1>VAT Report</h1>
      <div className={styles.filters}>
        <div><label>From Date:</label><input type="date" value={fromDate} onChange={e => setFromDate(e.target.value)} /></div>
        <div><label>To Date:</label><input type="date" value={toDate} onChange={e => setToDate(e.target.value)} /></div>
        <button onClick={handleGenerate} disabled={loading}>{loading ? 'Generating...' : 'Generate Report'}</button>
      </div>
      {report && (
        <div className={styles.report}>
          <h2>VAT Breakdown</h2>
          <table className={styles.table}>
            <thead><tr><th>VAT Rate</th><th>Revenue</th><th>VAT Amount</th><th>Lines</th></tr></thead>
            <tbody>{report.vatRates.map((r, i) => (
              <tr key={i}><td>{r.vatRate}%</td><td>€ {r.revenue.toFixed(2)}</td><td>€ {r.vatAmount.toFixed(2)}</td><td>{r.lineCount}</td></tr>
            ))}</tbody>
          </table>
          <div className={styles.totals}>
            <p><strong>Total Revenue:</strong> € {report.totalRevenue.toFixed(2)}</p>
            <p><strong>Total VAT:</strong> € {report.totalVat.toFixed(2)}</p>
            <p><strong>Total (incl. VAT):</strong> € {report.totalIncludingVat.toFixed(2)}</p>
            <p><strong>Invoices:</strong> {report.invoiceCount}</p>
          </div>
        </div>
      )}
    </div></AppShell></ProtectedRoute>
  );
}
