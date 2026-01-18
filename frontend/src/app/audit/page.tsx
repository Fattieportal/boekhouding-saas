'use client';
import { useEffect, useState } from 'react';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { AppShell } from '@/components/AppShell';
import { auditApi, AuditLog } from '@/lib/api';
import styles from './audit.module.css';

export default function AuditLogPage() {
  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [loading, setLoading] = useState(false);
  const [filter, setFilter] = useState('');

  useEffect(() => { fetchLogs(); }, [filter]);

  const fetchLogs = async () => {
    setLoading(true);
    try {
      const data = await auditApi.getLogs(0, 100, filter || undefined);
      setLogs(data);
    } catch (err: any) {
      alert(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <ProtectedRoute><AppShell><div className={styles.container}>
      <h1>Audit Log</h1>
      <div className={styles.filters}>
        <label>Filter by Action:</label>
        <select value={filter} onChange={e => setFilter(e.target.value)}>
          <option value="">All Actions</option>
          <option value="Create">Create</option>
          <option value="Update">Update</option>
          <option value="Delete">Delete</option>
          <option value="Post">Post</option>
        </select>
      </div>
      {loading && <div>Loading...</div>}
      {!loading && logs.length === 0 && <div>No audit logs found.</div>}
      {!loading && logs.length > 0 && (
        <table className={styles.table}>
          <thead><tr><th>Timestamp</th><th>Action</th><th>Entity Type</th><th>Entity ID</th><th>User</th><th>IP Address</th></tr></thead>
          <tbody>{logs.map(log => (
            <tr key={log.id}><td>{new Date(log.timestamp).toLocaleString()}</td><td>{log.action}</td><td>{log.entityType}</td><td>{log.entityId}</td><td>{log.actor.email}</td><td>{log.ipAddress || '-'}</td></tr>
          ))}</tbody>
        </table>
      )}
    </div></AppShell></ProtectedRoute>
  );
}
