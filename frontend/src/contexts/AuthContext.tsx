"use client";

import React, { createContext, useContext, useState, useEffect } from "react";
import { useRouter } from "next/navigation";

// Types
interface User {
  userId: string;
  email: string;
  role: string;
}

interface AuthContextType {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  checkAuth: () => boolean;
}

// Create context
const AuthContext = createContext<AuthContextType | undefined>(undefined);

// Provider component
export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const router = useRouter();

  const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5001";

  // Initialize auth state from localStorage on mount
  useEffect(() => {
    const storedToken = localStorage.getItem("token");
    const storedEmail = localStorage.getItem("email");
    const storedRole = localStorage.getItem("role");
    const storedUserId = localStorage.getItem("userId");

    if (storedToken && storedEmail && storedRole) {
      setToken(storedToken);
      setUser({
        userId: storedUserId || "",
        email: storedEmail,
        role: storedRole,
      });
    }

    setIsLoading(false);
  }, []);

  // Login function
  const login = async (email: string, password: string) => {
    try {
      const response = await fetch(`${API_URL}/api/auth/login`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ email, password }),
      });

      if (!response.ok) {
        throw new Error("Login failed");
      }

      const data = await response.json();

      // Store in state
      setToken(data.token);
      setUser({
        userId: data.userId,
        email: data.email,
        role: data.role,
      });

      // Store in localStorage
      localStorage.setItem("token", data.token);
      localStorage.setItem("userId", data.userId);
      localStorage.setItem("email", data.email);
      localStorage.setItem("role", data.role);

      // Fetch and store tenant info
      const tenantsResponse = await fetch(`${API_URL}/api/tenants/my`, {
        headers: {
          Authorization: `Bearer ${data.token}`,
        },
      });

      if (tenantsResponse.ok) {
        const tenants = await tenantsResponse.json();
        // API returns an array of tenants, take the first one
        if (Array.isArray(tenants) && tenants.length > 0) {
          const tenant = tenants[0];
          localStorage.setItem("tenantId", tenant.id);
          localStorage.setItem("tenantName", tenant.name);
          localStorage.setItem("tenantRole", tenant.role);
        }
      }
    } catch (error) {
      console.error("Login error:", error);
      throw error;
    }
  };

  // Logout function
  const logout = () => {
    // Clear state
    setToken(null);
    setUser(null);

    // Clear localStorage
    localStorage.removeItem("token");
    localStorage.removeItem("userId");
    localStorage.removeItem("email");
    localStorage.removeItem("role");
    localStorage.removeItem("tenantId");
    localStorage.removeItem("tenantName");
    localStorage.removeItem("tenantRole");

    // Redirect to login
    router.push("/login");
  };

  // Check if user is authenticated
  const checkAuth = (): boolean => {
    return !!token && !!user;
  };

  const value = {
    user,
    token,
    isAuthenticated: !!token && !!user,
    isLoading,
    login,
    logout,
    checkAuth,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// Hook to use auth context
export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
