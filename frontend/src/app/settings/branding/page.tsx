'use client';
import { useEffect, useState } from 'react';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { AppShell } from '@/components/AppShell';
import { brandingApi, TenantBranding } from '@/lib/api';
import styles from './branding.module.css';

export default function BrandingPage() {
  const [branding, setBranding] = useState<TenantBranding | null>(null);
  const [loading, setLoading] = useState(false);
  const [formData, setFormData] = useState({ logoUrl: '', primaryColor: '', secondaryColor: '', fontFamily: '', footerText: '' });

  useEffect(() => { fetchBranding(); }, []);

  const fetchBranding = async () => {
    try {
      const data = await brandingApi.get();
      setBranding(data);
      setFormData({ logoUrl: data.logoUrl || '', primaryColor: data.primaryColor || '', secondaryColor: data.secondaryColor || '', fontFamily: data.fontFamily || '', footerText: data.footerText || '' });
    } catch (err) { }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      await brandingApi.update(formData);
      alert('Branding updated!');
      fetchBranding();
    } catch (err: any) {
      alert(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <ProtectedRoute><AppShell><div className={styles.container}>
      <h1>Branding Settings</h1>
      <form onSubmit={handleSubmit} className={styles.form}>
        <div><label>Logo URL:</label><input type="url" value={formData.logoUrl} onChange={e => setFormData({...formData, logoUrl: e.target.value})} /></div>
        <div><label>Primary Color:</label><input type="color" value={formData.primaryColor} onChange={e => setFormData({...formData, primaryColor: e.target.value})} /></div>
        <div><label>Secondary Color:</label><input type="color" value={formData.secondaryColor} onChange={e => setFormData({...formData, secondaryColor: e.target.value})} /></div>
        <div><label>Font Family:</label><input type="text" value={formData.fontFamily} onChange={e => setFormData({...formData, fontFamily: e.target.value})} /></div>
        <div><label>Footer Text:</label><textarea value={formData.footerText} onChange={e => setFormData({...formData, footerText: e.target.value})} rows={3} /></div>
        <button type="submit" disabled={loading}>{loading ? 'Saving...' : 'Save Changes'}</button>
      </form>
    </div></AppShell></ProtectedRoute>
  );
}
