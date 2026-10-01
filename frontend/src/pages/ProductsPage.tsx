import { useState } from 'react';
import { useAuthStore } from '../store/authStore';
import { useProducts, useProductsByColour } from '../hooks/useProducts';
import CreateProductForm from '../components/CreateProductForm';
import ProductTable from '../components/ProductTable';
import ColourFilter from '../components/ColourFilter';

export default function ProductsPage() {
  const logout = useAuthStore((s) => s.logout);
  const [selectedColour, setSelectedColour] = useState('');
  const [showForm, setShowForm] = useState(false);

  const allProducts = useProducts();
  const colourProducts = useProductsByColour(selectedColour);

  const { data, isLoading } = selectedColour ? colourProducts : allProducts;

  return (
    <div style={s.page}>
      <header style={s.header}>
        <div>
          <h1 style={s.title}>Products</h1>
          <p style={s.count}>{data?.length ?? 0} product{data?.length !== 1 ? 's' : ''}</p>
        </div>
        <div style={s.actions}>
          <button onClick={() => setShowForm((v) => !v)} style={s.btnPrimary}>
            {showForm ? 'Cancel' : '+ Add Product'}
          </button>
          <button onClick={logout} style={s.btnSecondary}>
            Sign Out
          </button>
        </div>
      </header>

      {showForm && (
        <CreateProductForm onSuccess={() => setShowForm(false)} />
      )}

      <ColourFilter selected={selectedColour} onChange={setSelectedColour} />

      <ProductTable products={data ?? []} isLoading={isLoading} />
    </div>
  );
}

const s: Record<string, React.CSSProperties> = {
  page: { maxWidth: 960, margin: '0 auto', padding: '2rem 1rem' },
  header: { display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '1.5rem' },
  title: { margin: 0, fontSize: '1.75rem', fontWeight: 700, color: '#1a202c' },
  count: { margin: '4px 0 0', color: '#718096', fontSize: '0.875rem' },
  actions: { display: 'flex', gap: '0.75rem' },
  btnPrimary: { padding: '0.5rem 1.25rem', background: '#3182ce', color: '#fff', border: 'none', borderRadius: 6, fontWeight: 600, cursor: 'pointer', fontSize: '0.9rem' },
  btnSecondary: { padding: '0.5rem 1.25rem', background: '#fff', color: '#4a5568', border: '1px solid #e2e8f0', borderRadius: 6, fontWeight: 600, cursor: 'pointer', fontSize: '0.9rem' },
};
