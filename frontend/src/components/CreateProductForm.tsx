import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { COLOURS } from '../types';
import { useCreateProduct } from '../hooks/useProducts';

const schema = z.object({
  name: z.string().min(1, 'Name is required').max(200),
  description: z.string().max(1000).optional(),
  colour: z.string().min(1, 'Colour is required'),
  price: z.coerce.number().positive('Price must be positive'),
  stockQuantity: z.coerce.number().int().min(0, 'Stock cannot be negative'),
});

type FormValues = z.infer<typeof schema>;

interface Props {
  onSuccess?: () => void;
}

export default function CreateProductForm({ onSuccess }: Props) {
  const { mutate, isPending, isError, error, isSuccess, reset } = useCreateProduct();

  const { register, handleSubmit, reset: resetForm, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { stockQuantity: 0 },
  });

  const onSubmit = (data: FormValues) => {
    mutate(data as any, {
      onSuccess: () => {
        resetForm();
        reset();
        onSuccess?.();
      },
    });
  };

  const apiErrors: string[] = (error as any)?.response?.data?.errors ?? [];

  return (
    <form onSubmit={handleSubmit(onSubmit)} style={s.form}>
      <h3 style={s.heading}>Add New Product</h3>

      {isSuccess && <div style={s.success}>Product created successfully!</div>}
      {isError && (
        <div style={s.errorBox}>
          {apiErrors.length > 0
            ? apiErrors.map((e, i) => <div key={i}>{e}</div>)
            : 'Failed to create product.'}
        </div>
      )}

      <div style={s.grid}>
        <div style={s.field}>
          <label style={s.label}>Name *</label>
          <input {...register('name')} style={s.input} placeholder="Product name" />
          {errors.name && <span style={s.error}>{errors.name.message}</span>}
        </div>

        <div style={s.field}>
          <label style={s.label}>Colour *</label>
          <select {...register('colour')} style={s.input}>
            <option value="">Select colour…</option>
            {COLOURS.map((c) => <option key={c} value={c}>{c}</option>)}
          </select>
          {errors.colour && <span style={s.error}>{errors.colour.message}</span>}
        </div>

        <div style={s.field}>
          <label style={s.label}>Price (£) *</label>
          <input {...register('price')} type="number" step="0.01" style={s.input} placeholder="0.00" />
          {errors.price && <span style={s.error}>{errors.price.message}</span>}
        </div>

        <div style={s.field}>
          <label style={s.label}>Stock Quantity *</label>
          <input {...register('stockQuantity')} type="number" style={s.input} />
          {errors.stockQuantity && <span style={s.error}>{errors.stockQuantity.message}</span>}
        </div>
      </div>

      <div style={s.field}>
        <label style={s.label}>Description</label>
        <textarea {...register('description')} rows={2} style={{ ...s.input, resize: 'vertical' }} placeholder="Optional description" />
        {errors.description && <span style={s.error}>{errors.description.message}</span>}
      </div>

      <button type="submit" disabled={isPending} style={s.button}>
        {isPending ? 'Creating...' : 'Create Product'}
      </button>
    </form>
  );
}

const s: Record<string, React.CSSProperties> = {
  form: { background: '#fff', borderRadius: 8, border: '1px solid #e2e8f0', padding: '1.5rem', marginBottom: '1.5rem' },
  heading: { margin: '0 0 1rem', color: '#2d3748', fontSize: '1.1rem', fontWeight: 700 },
  grid: { display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '1rem' },
  field: { display: 'flex', flexDirection: 'column', gap: 4 },
  label: { fontSize: '0.8rem', fontWeight: 600, color: '#4a5568' },
  input: { padding: '0.5rem 0.75rem', border: '1px solid #e2e8f0', borderRadius: 6, fontSize: '0.9rem' },
  error: { fontSize: '0.75rem', color: '#e53e3e' },
  errorBox: { background: '#fff5f5', border: '1px solid #fed7d7', borderRadius: 6, padding: '0.5rem 0.75rem', color: '#c53030', fontSize: '0.85rem', marginBottom: '1rem' },
  success: { background: '#f0fff4', border: '1px solid #c6f6d5', borderRadius: 6, padding: '0.5rem 0.75rem', color: '#276749', fontSize: '0.85rem', marginBottom: '1rem' },
  button: { padding: '0.625rem 1.5rem', background: '#3182ce', color: '#fff', border: 'none', borderRadius: 6, fontWeight: 600, cursor: 'pointer', alignSelf: 'flex-start' },
};
