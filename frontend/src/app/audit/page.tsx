'use client';
import { useEffect, useState } from 'react';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { AppShell } from '@/components/AppShell';
import { auditApi, AuditLog } from '@/lib/api';
import styles from './audit.module.css';

type SortField = 'timestamp' | 'action' | 'entityType' | 'user';
type SortOrder = 'asc' | 'desc';

export default function AuditLogPage() {
  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [loading, setLoading] = useState(false);
  
  // Filters
  const [entityTypeFilter, setEntityTypeFilter] = useState('');
  const [actionFilter, setActionFilter] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  
  // Sorting
  const [sortBy, setSortBy] = useState<SortField>('timestamp');
  const [sortOrder, setSortOrder] = useState<SortOrder>('desc');
  
  // Pagination
  const [currentPage, setCurrentPage] = useState(1);
  const [itemsPerPage] = useState(50);

  const fetchLogs = async () => {
    setLoading(true);
    try {
      const skip = (currentPage - 1) * itemsPerPage;
      const params = new URLSearchParams();
      
      if (entityTypeFilter) params.append('entityType', entityTypeFilter);
      if (actionFilter) params.append('action', actionFilter);
      if (startDate) params.append('startDate', new Date(startDate).toISOString());
      if (endDate) params.append('endDate', new Date(endDate).toISOString());
      params.append('sortBy', sortBy);
      params.append('sortOrder', sortOrder);
      params.append('skip', skip.toString());
      params.append('take', itemsPerPage.toString());
      
      const data = await auditApi.getLogs(skip, itemsPerPage, params.toString());
      setLogs(data);
    } catch (err: any) {
      console.error('Failed to fetch audit logs:', err);
      alert(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { 
    fetchLogs(); 
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [entityTypeFilter, actionFilter, startDate, endDate, sortBy, sortOrder, currentPage]);

  const handleSort = (field: SortField) => {
    if (sortBy === field) {
      setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc');
    } else {
      setSortBy(field);
      setSortOrder('desc');
    }
  };

  const getSortIcon = (field: SortField) => {
    if (sortBy !== field) return '↕️';
    return sortOrder === 'asc' ? '↑' : '↓';
  };

  const clearFilters = () => {
    setEntityTypeFilter('');
    setActionFilter('');
    setStartDate('');
    setEndDate('');
    setCurrentPage(1);
  };

  const formatDiff = (diffJson: string | null) => {
    if (!diffJson) return null;
    try {
      const diff = JSON.parse(diffJson);
      return (
        <details className={styles.diffDetails}>
          <summary>View Details</summary>
          <pre>{JSON.stringify(diff, null, 2)}</pre>
        </details>
      );
    } catch {
      return diffJson;
    }
  };

  return (
    <ProtectedRoute>
      <AppShell>
        <div className={styles.container}>
          <div className={styles.header}>
            <h1>🔍 Audit Trail</h1>
            <p className={styles.subtitle}>Complete history of all system activities</p>
          </div>

          {/* Filters */}
          <div className={styles.filterPanel}>
            <div className={styles.filterGrid}>
              <div className={styles.filterGroup}>
                <label>Entity Type</label>
                <select value={entityTypeFilter} onChange={e => { setEntityTypeFilter(e.target.value); setCurrentPage(1); }}>
                  <option value="">All Types</option>
                  <option value="SalesInvoice">Sales Invoices</option>
                  <option value="Contact">Contacts</option>
                  <option value="JournalEntry">Journal Entries</option>
                  <option value="BankTransaction">Bank Transactions</option>
                  <option value="Account">Accounts</option>
                </select>
              </div>

              <div className={styles.filterGroup}>
                <label>Action</label>
                <select value={actionFilter} onChange={e => { setActionFilter(e.target.value); setCurrentPage(1); }}>
                  <option value="">All Actions</option>
                  <option value="CREATE">Create</option>
                  <option value="UPDATE">Update</option>
                  <option value="DELETE">Delete</option>
                  <option value="POST">Post</option>
                </select>
              </div>

              <div className={styles.filterGroup}>
                <label>Start Date</label>
                <input 
                  type="date" 
                  value={startDate} 
                  onChange={e => { setStartDate(e.target.value); setCurrentPage(1); }} 
                />
              </div>

              <div className={styles.filterGroup}>
                <label>End Date</label>
                <input 
                  type="date" 
                  value={endDate} 
                  onChange={e => { setEndDate(e.target.value); setCurrentPage(1); }} 
                />
              </div>

              <div className={styles.filterActions}>
                <button onClick={clearFilters} className={styles.clearButton}>
                  Clear Filters
                </button>
                <button onClick={fetchLogs} className={styles.refreshButton}>
                  🔄 Refresh
                </button>
              </div>
            </div>
          </div>

          {/* Stats */}
          <div className={styles.stats}>
            <div className={styles.statCard}>
              <span className={styles.statLabel}>Total Logs</span>
              <span className={styles.statValue}>{logs.length}</span>
            </div>
          </div>

          {/* Loading */}
          {loading && (
            <div className={styles.loading}>
              <div className={styles.spinner}></div>
              <p>Loading audit logs...</p>
            </div>
          )}

          {/* Empty State */}
          {!loading && logs.length === 0 && (
            <div className={styles.emptyState}>
              <div className={styles.emptyIcon}>📋</div>
              <h3>No audit logs found</h3>
              <p>Try adjusting your filters or check back later</p>
            </div>
          )}

          {/* Table */}
          {!loading && logs.length > 0 && (
            <div className={styles.tableContainer}>
              <table className={styles.table}>
                <thead>
                  <tr>
                    <th onClick={() => handleSort('timestamp')} className={styles.sortable}>
                      Timestamp {getSortIcon('timestamp')}
                    </th>
                    <th onClick={() => handleSort('action')} className={styles.sortable}>
                      Action {getSortIcon('action')}
                    </th>
                    <th onClick={() => handleSort('entityType')} className={styles.sortable}>
                      Entity Type {getSortIcon('entityType')}
                    </th>
                    <th>Entity ID</th>
                    <th onClick={() => handleSort('user')} className={styles.sortable}>
                      User {getSortIcon('user')}
                    </th>
                    <th>IP Address</th>
                    <th>Details</th>
                  </tr>
                </thead>
                <tbody>
                  {logs.map(log => (
                    <tr key={log.id} className={styles.logRow}>
                      <td className={styles.timestamp}>
                        {new Date(log.timestamp).toLocaleString('nl-NL', {
                          day: '2-digit',
                          month: '2-digit',
                          year: 'numeric',
                          hour: '2-digit',
                          minute: '2-digit',
                          second: '2-digit'
                        })}
                      </td>
                      <td>
                        <span className={`${styles.badge} ${styles[`badge${log.action}`]}`}>
                          {log.action}
                        </span>
                      </td>
                      <td className={styles.entityType}>{log.entityType}</td>
                      <td className={styles.entityId}>{log.entityId.substring(0, 8)}...</td>
                      <td className={styles.user}>{log.actor.email}</td>
                      <td className={styles.ip}>{log.ipAddress || '-'}</td>
                      <td className={styles.details}>
                        {formatDiff(log.diffJson ?? null)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {/* Pagination */}
          {!loading && logs.length > 0 && (
            <div className={styles.pagination}>
              <button 
                onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
                disabled={currentPage === 1}
                className={styles.pageButton}
              >
                ← Previous
              </button>
              <span className={styles.pageInfo}>Page {currentPage}</span>
              <button 
                onClick={() => setCurrentPage(p => p + 1)}
                disabled={logs.length < itemsPerPage}
                className={styles.pageButton}
              >
                Next →
              </button>
            </div>
          )}
        </div>
      </AppShell>
    </ProtectedRoute>
  );
}
