'use client';
import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { AppShell } from '@/components/AppShell';
import { contactsApi, Contact } from '@/lib/api';
import styles from './edit.module.css';

export default function EditContactPage() {
  const params = useParams();
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [contact, setContact] = useState<Contact | null>(null);
  const [formData, setFormData] = useState({ type: 0, displayName: '', email: '', phone: '', addressLine1: '', addressLine2: '', postalCode: '', city: '', country: '', vatNumber: '', kvK: '' });

  useEffect(() => { if (params.id) fetchContact(); }, [params.id]);

  const fetchContact = async () => {
    try {
      const data = await contactsApi.getById(params.id as string);
      setContact(data);
      setFormData({ type: data.type, displayName: data.displayName, email: data.email || '', phone: data.phone || '', addressLine1: data.addressLine1 || '', addressLine2: data.addressLine2 || '', postalCode: data.postalCode || '', city: data.city || '', country: data.country || '', vatNumber: data.vatNumber || '', kvK: data.kvK || '' });
    } catch (err: any) {
      alert(err.message);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!contact) return;
    setLoading(true);
    try {
      await contactsApi.update(contact.id, { ...formData, isActive: contact.isActive });
      router.push('/contacts');
    } catch (err: any) {
      alert(err.message);
    } finally {
      setLoading(false);
    }
  };

  if (!contact) return (<ProtectedRoute><AppShell><div>Loading...</div></AppShell></ProtectedRoute>);

  return (
    <ProtectedRoute><AppShell><div className={styles.container}>
      <h1>Edit Contact</h1>
      <form onSubmit={handleSubmit} className={styles.form}>
        <div><label>Display Name *</label><input type="text" value={formData.displayName} onChange={e => setFormData({...formData, displayName: e.target.value})} required /></div>
        <div><label>Type *</label><select value={formData.type} onChange={e => setFormData({...formData, type: parseInt(e.target.value)})}><option value={0}>Customer</option><option value={1}>Supplier</option><option value={2}>Both</option></select></div>
        <div><label>Email</label><input type="email" value={formData.email} onChange={e => setFormData({...formData, email: e.target.value})} /></div>
        <div><label>Phone</label><input type="tel" value={formData.phone} onChange={e => setFormData({...formData, phone: e.target.value})} /></div>
        <div><label>Address Line 1</label><input type="text" value={formData.addressLine1} onChange={e => setFormData({...formData, addressLine1: e.target.value})} /></div>
        <div><label>Address Line 2</label><input type="text" value={formData.addressLine2} onChange={e => setFormData({...formData, addressLine2: e.target.value})} /></div>
        <div><label>Postal Code</label><input type="text" value={formData.postalCode} onChange={e => setFormData({...formData, postalCode: e.target.value})} /></div>
        <div><label>City</label><input type="text" value={formData.city} onChange={e => setFormData({...formData, city: e.target.value})} /></div>
        <div><label>Country</label><input type="text" value={formData.country} onChange={e => setFormData({...formData, country: e.target.value})} /></div>
        <div><label>VAT Number</label><input type="text" value={formData.vatNumber} onChange={e => setFormData({...formData, vatNumber: e.target.value})} /></div>
        <div><label>KvK Number</label><input type="text" value={formData.kvK} onChange={e => setFormData({...formData, kvK: e.target.value})} /></div>
        <div className={styles.actions}><button type="button" onClick={() => router.push('/contacts')}>Cancel</button><button type="submit" disabled={loading}>{loading ? 'Saving...' : 'Save Changes'}</button></div>
      </form>
    </div></AppShell></ProtectedRoute>
  );
}
