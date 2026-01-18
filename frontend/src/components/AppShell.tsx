"use client";

import { useEffect } from "react";
import { useAuth } from "@/contexts/AuthContext";
import { useTenant } from "@/contexts/TenantContext";
import { useRouter, usePathname } from "next/navigation";
import styles from "./AppShell.module.css";

interface AppShellProps {
  children: React.ReactNode;
}

export function AppShell({ children }: AppShellProps) {
  const { user, logout } = useAuth();
  const { tenant, tenants, fetchTenants, switchTenant } = useTenant();
  const router = useRouter();
  const pathname = usePathname();

  // Fetch all tenants on mount
  useEffect(() => {
    if (user) {
      fetchTenants();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [user]);

  const navItems = [
    { label: "Dashboard", path: "/" },
    { label: "Contacts", path: "/contacts" },
    { label: "Invoices", path: "/invoices" },
    { label: "Banking", path: "/banking/connections" },
    { label: "VAT Report", path: "/reports/vat" },
    { label: "Audit Log", path: "/audit" },
    { label: "Settings", path: "/settings/branding" },
  ];

  const handleLogout = () => {
    if (confirm("Are you sure you want to log out?")) {
      logout();
    }
  };

  return (
    <div className={styles.shell}>
      {/* Top Navigation */}
      <header className={styles.header}>
        <div className={styles.headerLeft}>
          <h1 className={styles.logo}>📊 Boekhouding SaaS</h1>
        </div>

        <div className={styles.headerRight}>
          {/* Tenant Switcher */}
          {tenant && tenants.length > 1 && (
            <div className={styles.tenantSwitcher}>
              <label htmlFor="tenant-select">Tenant:</label>
              <select
                id="tenant-select"
                value={tenant.id}
                onChange={(e) => switchTenant(e.target.value)}
                className={styles.tenantSelect}
              >
                {tenants.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.name}
                  </option>
                ))}
              </select>
            </div>
          )}

          {/* Tenant Info (Single tenant or current selection) */}
          {tenant && tenants.length <= 1 && (
            <div className={styles.tenantInfo}>
              <span className={styles.tenantIcon}>🏢</span>
              <div className={styles.tenantDetails}>
                <div className={styles.tenantName}>{tenant.name}</div>
                <div className={styles.tenantRole}>{tenant.role}</div>
              </div>
            </div>
          )}

          {/* User Menu */}
          <div className={styles.userMenu}>
            <span className={styles.userIcon}>👤</span>
            <div className={styles.userDetails}>
              <div className={styles.userEmail}>{user?.email}</div>
              <button onClick={handleLogout} className={styles.logoutButton}>
                Logout
              </button>
            </div>
          </div>
        </div>
      </header>

      {/* Side Navigation */}
      <div className={styles.layout}>
        <nav className={styles.sidebar}>
          <ul className={styles.navList}>
            {navItems.map((item) => (
              <li key={item.path}>
                <button
                  onClick={() => router.push(item.path)}
                  className={`${styles.navItem} ${
                    pathname === item.path ? styles.navItemActive : ""
                  }`}
                >
                  {item.label}
                </button>
              </li>
            ))}
          </ul>
        </nav>

        {/* Main Content */}
        <main className={styles.main}>{children}</main>
      </div>
    </div>
  );
}
