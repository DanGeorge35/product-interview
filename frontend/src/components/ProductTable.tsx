import type { ProductDto } from '../types';

interface Props {
  products: ProductDto[];
  isLoading: boolean;
}

export default function ProductTable({ products, isLoading }: Props) {
  if (isLoading) return <p style={s.status}>Loading products...</p>;
  if (products.length === 0) return <p style={s.status}>No products found.</p>;

  return (
    <div style={s.wrapper}>
      <table style={s.table}>
        <thead>
          <tr>
            {['Name', 'Colour', 'Price', 'Stock', 'Created'].map((h) => (
              <th key={h} style={s.th}>{h}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {products.map((p) => (
            <tr key={p.id} style={s.tr}>
              <td style={s.td}>
                <div style={s.name}>{p.name}</div>
                {p.description && <div style={s.desc}>{p.description}</div>}
              </td>
              <td style={s.td}>
                <span style={{ ...s.badge, background: colourBg(p.colour) }}>
                  {p.colour}
                </span>
              </td>
              <td style={s.td}>£{p.price.toFixed(2)}</td>
              <td style={s.td}>{p.stockQuantity}</td>
              <td style={s.td}>{new Date(p.createdAt).toLocaleDateString()}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function colourBg(colour: string): string {
  const map: Record<string, string> = {
    Red: '#fed7d7', Blue: '#bee3f8', Green: '#c6f6d5', Yellow: '#fefcbf',
    Black: '#e2e8f0', White: '#f7fafc', Orange: '#feebc8', Purple: '#e9d8fd',
    Pink: '#fed7e2', Brown: '#fbd38d', Grey: '#e2e8f0',
  };
  return map[colour] ?? '#edf2f7';
}

const s: Record<string, React.CSSProperties> = {
  wrapper: { overflowX: 'auto', borderRadius: 8, border: '1px solid #e2e8f0' },
  table: { width: '100%', borderCollapse: 'collapse', background: '#fff' },
  th: { padding: '0.75rem 1rem', textAlign: 'left', fontSize: '0.75rem', fontWeight: 700, textTransform: 'uppercase', color: '#718096', borderBottom: '2px solid #e2e8f0' },
  tr: { borderBottom: '1px solid #f7fafc' },
  td: { padding: '0.75rem 1rem', fontSize: '0.9rem', color: '#2d3748', verticalAlign: 'top' },
  name: { fontWeight: 600 },
  desc: { fontSize: '0.8rem', color: '#718096', marginTop: 2 },
  badge: { display: 'inline-block', padding: '0.2rem 0.6rem', borderRadius: 12, fontSize: '0.8rem', fontWeight: 600 },
  status: { color: '#718096', textAlign: 'center', padding: '2rem' },
};
