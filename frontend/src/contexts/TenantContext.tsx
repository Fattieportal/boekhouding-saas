"use client";

import React, { createContext, useContext, useState, useEffect } from "react";
import { useAuth } from "./AuthContext";

// Types
interface Tenant {
  id: string;
  name: string;
  role: string;
}

interface TenantContextType {
  tenant: Tenant | null;
  tenants: Tenant[];
  isLoading: boolean;
  setTenant: (tenant: Tenant | null) => void;
  clearTenant: () => void;
  fetchTenants: () => Promise<void>;
  switchTenant: (tenantId: string) => void;
}

// Create context
const TenantContext = createContext<TenantContextType | undefined>(undefined);

// Provider component
export function TenantProvider({ children }: { children: React.ReactNode }) {
  const [tenant, setTenantState] = useState<Tenant | null>(null);
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const { isAuthenticated, token } = useAuth();

  // Initialize tenant from localStorage on mount
  useEffect(() => {
    if (isAuthenticated) {
      const storedTenantId = localStorage.getItem("tenantId");
      const storedTenantName = localStorage.getItem("tenantName");
      const storedTenantRole = localStorage.getItem("tenantRole");

      if (storedTenantId && storedTenantName) {
        setTenantState({
          id: storedTenantId,
          name: storedTenantName,
          role: storedTenantRole || "User",
        });
      }
    } else {
      setTenantState(null);
      setTenants([]);
    }
    setIsLoading(false);
  }, [isAuthenticated]);

  // Fetch all tenants for current user
  const fetchTenants = async () => {
    if (!token) return;
    
    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001'}/api/tenants/my`, {
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });

      if (response.ok) {
        const data = await response.json();
        setTenants(Array.isArray(data) ? data : [data]);
      }
    } catch (err) {
      console.error('Failed to fetch tenants:', err);
    }
  };

  // Set tenant (also saves to localStorage)
  const setTenant = (newTenant: Tenant | null) => {
    setTenantState(newTenant);

    if (newTenant) {
      localStorage.setItem("tenantId", newTenant.id);
      localStorage.setItem("tenantName", newTenant.name);
      localStorage.setItem("tenantRole", newTenant.role);
    } else {
      localStorage.removeItem("tenantId");
      localStorage.removeItem("tenantName");
      localStorage.removeItem("tenantRole");
    }
  };

  // Switch to different tenant
  const switchTenant = (tenantId: string) => {
    const selectedTenant = tenants.find(t => t.id === tenantId);
    if (selectedTenant) {
      setTenant(selectedTenant);
      // Reload page to refresh all data with new tenant context
      window.location.reload();
    }
  };

  // Clear tenant
  const clearTenant = () => {
    setTenant(null);
  };

  const value = {
    tenant,
    tenants,
    isLoading,
    setTenant,
    clearTenant,
    fetchTenants,
    switchTenant,
  };

  return (
    <TenantContext.Provider value={value}>{children}</TenantContext.Provider>
  );
}

// Hook to use tenant context
export function useTenant() {
  const context = useContext(TenantContext);
  if (context === undefined) {
    throw new Error("useTenant must be used within a TenantProvider");
  }
  return context;
}
