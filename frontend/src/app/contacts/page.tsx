'use client';
import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { ProtectedRoute } from '@/components/ProtectedRoute';
import { AppShell } from '@/components/AppShell';
import { contactsApi, Contact } from '@/lib/api';
import styles from './contacts.module.css';

export default function ContactsPage() {
  const [contacts, setContacts] = useState<Contact[]>([]);
  const [filteredContacts, setFilteredContacts] = useState<Contact[]>([]);
  const [loading, setLoading] = useState(false);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [searchTerm, setSearchTerm] = useState('');
  const router = useRouter();

  useEffect(() => { fetchContacts(); }, [page]);

  useEffect(() => {
    if (!searchTerm.trim()) {
      setFilteredContacts(contacts);
    } else {
      const term = searchTerm.toLowerCase();
      setFilteredContacts(contacts.filter(c => 
        c.displayName.toLowerCase().includes(term) ||
        (c.email && c.email.toLowerCase().includes(term)) ||
        (c.phone && c.phone.includes(term)) ||
        (c.vatNumber && c.vatNumber.toLowerCase().includes(term))
      ));
    }
  }, [contacts, searchTerm]);

  const fetchContacts = async () => {
    try {
      setLoading(true);
      const data = await contactsApi.getAll(page, 25);
      setContacts(data.items);
      setTotalCount(data.totalCount);
    } catch (err: any) {
      alert(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchContacts();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page]);

  const handleDelete = async (id: string) => {
    if (!confirm('Delete contact?')) return;
    try {
      await contactsApi.delete(id);
      fetchContacts();
    } catch (err: any) {
      alert(err.message);
    }
  };

  const typeLabels = ['Customer', 'Supplier', 'Both'];

  return (
    <ProtectedRoute><AppShell><div className={styles.container}>
      <div className={styles.header}>
        <h1>Contacts</h1>
        <div className={styles.headerActions}>
          <input
            type="text"
            placeholder="Search contacts..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className={styles.searchInput}
          />
          <button onClick={() => router.push('/contacts/new')} className={styles.createButton}>+ Create Contact</button>
        </div>
      </div>
      {loading && <div>Loading...</div>}
      {!loading && filteredContacts.length === 0 && <div>No contacts found.</div>}
      {!loading && filteredContacts.length > 0 && (
        <table className={styles.table}>
          <thead><tr><th>Name</th><th>Type</th><th>Email</th><th>Phone</th><th>VAT Number</th><th>Actions</th></tr></thead>
          <tbody>{filteredContacts.map(c => (
            <tr key={c.id}><td><button onClick={() => router.push(`/contacts/${c.id}`)}>{c.displayName}</button></td><td>{typeLabels[c.type]}</td><td>{c.email || '-'}</td><td>{c.phone || '-'}</td><td>{c.vatNumber || '-'}</td><td><button onClick={() => router.push(`/contacts/${c.id}`)}>Edit</button><button onClick={() => handleDelete(c.id)}>Delete</button></td></tr>
          ))}</tbody>
        </table>
      )}
      <div className={styles.pagination}><button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}>Previous</button><span>Page {page}</span><button onClick={() => setPage(p => p + 1)} disabled={contacts.length < 25}>Next</button></div>
    </div></AppShell></ProtectedRoute>
  );
}
