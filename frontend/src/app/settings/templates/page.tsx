'use client';
import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { AppShell } from '@/components/AppShell';
import { templatesApi, InvoiceTemplate } from '@/lib/api';
import styles from './templates.module.css';

export default function TemplatesPage() {
  const router = useRouter();
  const [templates, setTemplates] = useState<InvoiceTemplate[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => { fetchTemplates(); }, []);

  const fetchTemplates = async () => {
    try {
      setLoading(true);
      const data = await templatesApi.getAll();
      setTemplates(data);
    } catch (err: any) {
      alert(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <ProtectedRoute><AppShell><div className={styles.container}>
      <div className={styles.header}><h1>Invoice Templates</h1></div>
      {loading && <div>Loading...</div>}
      {!loading && templates.length === 0 && <div>No templates found.</div>}
      {!loading && templates.length > 0 && (
        <div className={styles.grid}>{templates.map(t => (
          <div key={t.id} className={styles.card}>
            <h3>{t.name}</h3>
            <p>{t.isDefault && <span className={styles.badge}>Default</span>}</p>
            <button onClick={() => router.push(`/settings/templates/${t.id}`)} className={styles.editBtn}>Edit Template</button>
          </div>
        ))}</div>
      )}
    </div></AppShell></ProtectedRoute>
  );
}
