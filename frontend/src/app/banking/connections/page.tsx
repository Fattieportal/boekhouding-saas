'use client';
import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { AppShell } from '@/components/AppShell';
import { bankApi, BankConnection } from '@/lib/api';
import styles from './connections.module.css';

export default function BankConnectionsPage() {
  const [connections, setConnections] = useState<BankConnection[]>([]);
  const [loading, setLoading] = useState(false);
  const router = useRouter();

  useEffect(() => { fetchConnections(); }, []);

  const fetchConnections = async () => {
    try {
      setLoading(true);
      const data = await bankApi.getConnections();
      setConnections(data);
    } catch (err: any) {
      alert(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleConnect = async () => {
    try {
      const result = await bankApi.connect('GoCardless');
      if (result.authorizationUrl) {
        window.location.href = result.authorizationUrl;
      }
    } catch (err: any) {
      alert(err.message);
    }
  };

  const handleSync = async (id: string) => {
    try {
      const from = new Date(Date.now() - 90 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
      const to = new Date().toISOString().split('T')[0];
      const result = await bankApi.sync(id, from, to);
      alert(`Synced: ${result.transactionsImported} imported, ${result.transactionsUpdated} updated`);
      fetchConnections();
    } catch (err: any) {
      alert(err.message);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Delete connection?')) return;
    try {
      await bankApi.deleteConnection(id);
      fetchConnections();
    } catch (err: any) {
      alert(err.message);
    }
  };

  const statusLabels = ['Pending', 'Active', 'Expired'];

  return (
    <ProtectedRoute><AppShell><div className={styles.container}>
      <div className={styles.header}><h1>Bank Connections</h1><button onClick={handleConnect} className={styles.connectBtn}>+ Connect Bank</button></div>
      {loading && <div>Loading...</div>}
      {!loading && connections.length === 0 && <div>No bank connections. Connect your first bank account!</div>}
      {!loading && connections.length > 0 && (
        <table className={styles.table}>
          <thead><tr><th>Bank</th><th>IBAN</th><th>Status</th><th>Last Synced</th><th>Actions</th></tr></thead>
          <tbody>{connections.map(c => (
            <tr key={c.id}><td>{c.bankName}</td><td>{c.ibanMasked}</td><td>{statusLabels[c.status]}</td><td>{c.lastSyncedAt ? new Date(c.lastSyncedAt).toLocaleDateString() : 'Never'}</td><td><button onClick={() => handleSync(c.id)}>Sync</button><button onClick={() => handleDelete(c.id)}>Delete</button></td></tr>
          ))}</tbody>
        </table>
      )}
    </div></AppShell></ProtectedRoute>
  );
}
