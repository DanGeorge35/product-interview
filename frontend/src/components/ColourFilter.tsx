import { COLOURS } from '../types';

interface Props {
  selected: string;
  onChange: (colour: string) => void;
}

export default function ColourFilter({ selected, onChange }: Props) {
  return (
    <div style={s.wrapper}>
      <span style={s.label}>Filter by colour:</span>
      <button
        style={{ ...s.btn, ...(selected === '' ? s.active : {}) }}
        onClick={() => onChange('')}
      >
        All
      </button>
      {COLOURS.map((c) => (
        <button
          key={c}
          style={{ ...s.btn, ...(selected === c ? s.active : {}) }}
          onClick={() => onChange(c)}
        >
          {c}
        </button>
      ))}
    </div>
  );
}

const s: Record<string, React.CSSProperties> = {
  wrapper: { display: 'flex', flexWrap: 'wrap', gap: '0.4rem', alignItems: 'center', marginBottom: '1rem' },
  label: { fontSize: '0.85rem', fontWeight: 600, color: '#4a5568', marginRight: 4 },
  btn: { padding: '0.25rem 0.75rem', border: '1px solid #e2e8f0', borderRadius: 20, fontSize: '0.8rem', cursor: 'pointer', background: '#fff', color: '#4a5568' },
  active: { background: '#3182ce', color: '#fff', border: '1px solid #3182ce' },
};
