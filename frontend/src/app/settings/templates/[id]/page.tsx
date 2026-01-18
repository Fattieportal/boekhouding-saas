'use client';
import { useState, useEffect } from 'react';
import { useRouter, useParams } from 'next/navigation';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { AppShell } from '@/components/AppShell';
import { templatesApi } from '@/lib/api';
import styles from './template.module.css';

interface InvoiceTemplate { id: string; name: string; htmlTemplate: string; cssTemplate: string; isDefault: boolean; }

export default function TemplateEditPage() {
  const router = useRouter();
  const params = useParams();
  const id = params.id as string;
  const [template, setTemplate] = useState<InvoiceTemplate | null>(null);
  const [name, setName] = useState('');
  const [html, setHtml] = useState('');
  const [css, setCss] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadTemplate();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  const loadTemplate = async () => {
    try {
      setLoading(true);
      const data = await templatesApi.getById(id);
      setTemplate(data);
      setName(data.name);
      setHtml(data.htmlTemplate);
      setCss(data.cssTemplate);
    } catch (err: any) {
      alert(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    try {
      await templatesApi.update(id, { name, htmlTemplate: html, cssTemplate: css });
      alert('Template saved!');
      router.push('/settings/templates');
    } catch (err: any) {
      alert(err.message);
    }
  };

  if (loading) return (
    <ProtectedRoute>
      <AppShell>
        <div className={styles.container}>Loading...</div>
      </AppShell>
    </ProtectedRoute>
  );
  
  if (!template) return (
    <ProtectedRoute>
      <AppShell>
        <div className={styles.container}>Template not found</div>
      </AppShell>
    </ProtectedRoute>
  );

  return (
    <ProtectedRoute>
      <AppShell>
        <div className={styles.container}>
          <div className={styles.header}>
            <h1>Edit Template: {template.name}</h1>
            <div className={styles.actions}>
              <button onClick={handleSave} className={styles.saveBtn}>Save</button>
              <button onClick={() => router.push('/settings/templates')} className={styles.cancelBtn}>Cancel</button>
            </div>
          </div>

          <div className={styles.form}>
            <div className={styles.field}>
              <label>Template Name</label>
              <input type="text" value={name} onChange={(e) => setName(e.target.value)} />
            </div>

            <div className={styles.editors}>
              <div className={styles.editor}>
                <label>HTML Template</label>
                <textarea value={html} onChange={(e) => setHtml(e.target.value)} rows={20} placeholder="HTML content..." />
                <small>Available placeholders: CompanyName, InvoiceNumber, InvoiceDate, TotalAmount</small>
              </div>

              <div className={styles.editor}>
                <label>CSS Styles</label>
                <textarea value={css} onChange={(e) => setCss(e.target.value)} rows={20} placeholder="CSS styles..." />
                <small>Define styles for your invoice layout</small>
              </div>
            </div>

            <div className={styles.info}>
              <p><strong>Status:</strong> {template.isDefault ? 'Default Template' : 'Custom Template'}</p>
              <p><strong>Preview:</strong> Save changes</p>
            </div>
          </div>
        </div>
      </AppShell>
    </ProtectedRoute>
  );
}
