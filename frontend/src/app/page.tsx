'use client';

import { ProtectedRoute } from '@/components/ProtectedRoute';
import { AppShell } from '@/components/AppShell';
import { useAuth } from '@/contexts/AuthContext';
import { useTenant } from '@/contexts/TenantContext';
import { useRouter } from 'next/navigation';
import { useState, useEffect } from 'react';
import { api } from '@/lib/api';
import styles from './page.module.css';

interface DashboardData {
  invoices: {
    unpaidCount: number;
    overdueCount: number;
    openAmountTotal: number;
    paidThisPeriodAmount: number;
    paidThisPeriodCount: number;
  };
  revenue: {
    revenueExclThisPeriod: number;
    vatThisPeriod: number;
    revenueInclThisPeriod: number;
  };
  bank: {
    lastSyncAt: string | null;
    unmatchedTransactionsCount: number;
    matchedTransactionsCount: number;
  };
  activity: Array<{
    timestamp: string;
    actorEmail: string;
    action: string;
    entityType: string;
    entityId: string;
    label: string;
  }>;
  topCustomers: Array<{
    contactId: string;
    contactName: string;
    totalRevenue: number;
    invoiceCount: number;
  }>;
}

export default function DashboardPage() {
  const { user } = useAuth();
  const { tenant } = useTenant();
  const router = useRouter();
  const [dashboard, setDashboard] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchDashboard = async () => {
      try {
        setLoading(true);
        // Get current month date range
        const now = new Date();
        const from = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().split('T')[0];
        const to = now.toISOString().split('T')[0];
        
        const data = await api.get<DashboardData>(`/api/dashboard?from=${from}&to=${to}`);
        setDashboard(data);
        setError(null);
      } catch (err: any) {
        console.error('Failed to fetch dashboard:', err);
        setError(err.message || 'Failed to load dashboard data');
      } finally {
        setLoading(false);
      }
    };

    if (tenant) {
      fetchDashboard();
    }
  }, [tenant]);

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('nl-NL', {
      style: 'currency',
      currency: 'EUR'
    }).format(amount);
  };

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleString('nl-NL', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const formatRelativeTime = (dateStr: string) => {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) return 'just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    return formatDate(dateStr);
  };

  return (
    <ProtectedRoute>
      <AppShell>
        <div className={styles.container}>
          <div className={styles.welcomeCard}>
            <h1>Welcome back!</h1>
            <p className={styles.userInfo}>
              Logged in as: <strong>{user?.email}</strong>
            </p>
            {tenant && (
              <p className={styles.tenantInfo}>
                Current tenant: <strong>{tenant.name}</strong>
              </p>
            )}
          </div>

          {error && (
            <div className={styles.errorCard}>
              <p>⚠️ {error}</p>
            </div>
          )}

          {loading ? (
            <div className={styles.loadingCard}>
              <p>Loading dashboard...</p>
            </div>
          ) : dashboard && (
            <>
              <div className={styles.statsGrid}>
                <div className={styles.statCard}>
                  <div className={styles.statIcon}>📄</div>
                  <div className={styles.statContent}>
                    <h3>Unpaid Invoices</h3>
                    <p className={styles.statValue}>{dashboard.invoices.unpaidCount}</p>
                    <p className={styles.statLabel}>
                      {formatCurrency(dashboard.invoices.openAmountTotal)} outstanding
                    </p>
                  </div>
                </div>

                <div className={`${styles.statCard} ${dashboard.invoices.overdueCount > 0 ? styles.statCardWarning : ''}`}>
                  <div className={styles.statIcon}>⏰</div>
                  <div className={styles.statContent}>
                    <h3>Overdue</h3>
                    <p className={styles.statValue}>{dashboard.invoices.overdueCount}</p>
                    <p className={styles.statLabel}>
                      {dashboard.invoices.overdueCount > 0 ? 'Needs attention!' : 'All current'}
                    </p>
                  </div>
                </div>

                <div className={styles.statCard}>
                  <div className={styles.statIcon}>💰</div>
                  <div className={styles.statContent}>
                    <h3>Revenue</h3>
                    <p className={styles.statValue}>
                      {formatCurrency(dashboard.revenue.revenueInclThisPeriod)}
                    </p>
                    <p className={styles.statLabel}>This month (incl. VAT)</p>
                  </div>
                </div>

                <div className={styles.statCard}>
                  <div className={styles.statIcon}>📊</div>
                  <div className={styles.statContent}>
                    <h3>VAT</h3>
                    <p className={styles.statValue}>
                      {formatCurrency(dashboard.revenue.vatThisPeriod)}
                    </p>
                    <p className={styles.statLabel}>This month</p>
                  </div>
                </div>

                <div className={styles.statCard}>
                  <div className={styles.statIcon}>🏦</div>
                  <div className={styles.statContent}>
                    <h3>Bank</h3>
                    <p className={styles.statValue}>{dashboard.bank.unmatchedTransactionsCount}</p>
                    <p className={styles.statLabel}>Unmatched transactions</p>
                  </div>
                </div>

                <div className={styles.statCard}>
                  <div className={styles.statIcon}>✅</div>
                  <div className={styles.statContent}>
                    <h3>Paid</h3>
                    <p className={styles.statValue}>{dashboard.invoices.paidThisPeriodCount}</p>
                    <p className={styles.statLabel}>
                      {formatCurrency(dashboard.invoices.paidThisPeriodAmount)} this month
                    </p>
                  </div>
                </div>
              </div>

              <div className={styles.contentGrid}>
                <div className={styles.activitySection}>
                  <h2>Recent Activity</h2>
                  {dashboard.activity.length === 0 ? (
                    <p className={styles.emptyState}>No recent activity</p>
                  ) : (
                    <div className={styles.activityList}>
                      {dashboard.activity.map((item, idx) => (
                        <div 
                          key={idx} 
                          className={styles.activityItem}
                          onClick={() => {
                            // Navigate to entity detail based on entityType
                            if (item.entityType === 'SalesInvoice') {
                              router.push(`/invoices/${item.entityId}`);
                            } else if (item.entityType === 'BankTransaction') {
                              router.push(`/banking/transactions`);
                            } else if (item.entityType === 'Contact') {
                              router.push(`/contacts`);
                            }
                          }}
                          style={{ cursor: 'pointer' }}
                        >
                          <div className={styles.activityContent}>
                            <p className={styles.activityLabel}>{item.label}</p>
                            <p className={styles.activityMeta}>
                              by {item.actorEmail} · {formatRelativeTime(item.timestamp)}
                            </p>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>

                <div className={styles.customersSection}>
                  <h2>Top Customers</h2>
                  {dashboard.topCustomers.length === 0 ? (
                    <p className={styles.emptyState}>No customer data yet</p>
                  ) : (
                    <div className={styles.customersList}>
                      {dashboard.topCustomers.map((customer) => (
                        <div 
                          key={customer.contactId} 
                          className={styles.customerItem}
                          onClick={() => router.push(`/contacts`)}
                          style={{ cursor: 'pointer' }}
                        >
                          <div className={styles.customerInfo}>
                            <p className={styles.customerName}>{customer.contactName}</p>
                            <p className={styles.customerMeta}>
                              {customer.invoiceCount} invoice{customer.invoiceCount !== 1 ? 's' : ''}
                            </p>
                          </div>
                          <div className={styles.customerRevenue}>
                            {formatCurrency(customer.totalRevenue)}
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>
            </>
          )}

          <div className={styles.actionsSection}>
            <h2>Quick Actions</h2>
            <div className={styles.actionsGrid}>
              <button
                className={styles.actionCard}
                onClick={() => router.push('/invoices/new')}
              >
                <span className={styles.actionIcon}>➕</span>
                <span className={styles.actionLabel}>Create Invoice</span>
              </button>

              <button
                className={styles.actionCard}
                onClick={() => router.push('/invoices')}
              >
                <span className={styles.actionIcon}>📋</span>
                <span className={styles.actionLabel}>View Invoices</span>
              </button>

              <button
                className={styles.actionCard}
                onClick={() => router.push('/banking/connections')}
              >
                <span className={styles.actionIcon}>🔗</span>
                <span className={styles.actionLabel}>Bank Connections</span>
              </button>

              <button
                className={styles.actionCard}
                onClick={() => router.push('/reports/vat')}
              >
                <span className={styles.actionIcon}>📈</span>
                <span className={styles.actionLabel}>VAT Report</span>
              </button>
            </div>
          </div>
        </div>
      </AppShell>
    </ProtectedRoute>
  );
}
