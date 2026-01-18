"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/contexts/AuthContext";
import styles from "./login.module.css";

export default function LoginPage() {
  const [email, setEmail] = useState("admin@demo.local");
  const [password, setPassword] = useState("Admin123!");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const router = useRouter();
  const { login } = useAuth();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError("");

    try {
      await login(email, password);
      // Redirect to dashboard after successful login
      router.push("/");
    } catch (err) {
      setError("Invalid email or password");
    } finally {
      setLoading(false);
    }
  };

  const handleDevLogin = () => {
    setEmail("admin@demo.local");
    setPassword("Admin123!");
  };

  return (
    <div className={styles.container}>
      <div className={styles.loginBox}>
        <h1>Boekhouding SaaS</h1>
        <p className={styles.subtitle}>Sign in to your account</p>

        {error && <div className={styles.error}>{error}</div>}

        <form onSubmit={handleSubmit} className={styles.form}>
          <div className={styles.formGroup}>
            <label htmlFor="email">Email</label>
            <input
              type="email"
              id="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              autoComplete="email"
            />
          </div>

          <div className={styles.formGroup}>
            <label htmlFor="password">Password</label>
            <input
              type="password"
              id="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              autoComplete="current-password"
            />
          </div>

          <button type="submit" disabled={loading} className={styles.submitButton}>
            {loading ? "Signing in..." : "Sign In"}
          </button>
        </form>

        <div className={styles.hint}>
          <p>Demo credentials:</p>
          <p><strong>Email:</strong> admin@demo.local</p>
          <p><strong>Password:</strong> Admin123!</p>
          <button onClick={handleDevLogin} className={styles.devButton}>
            🚀 Quick Fill Demo Credentials
          </button>
        </div>
      </div>
    </div>
  );
}
