import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useLogin } from '../hooks/useAuth';

const schema = z.object({
  username: z.string().min(1, 'Username is required'),
  password: z.string().min(1, 'Password is required'),
});

type FormValues = z.infer<typeof schema>;

export default function LoginPage() {
  const { mutate: login, isPending, isError, error } = useLogin();

  const { register, handleSubmit, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
  });

  const onSubmit = (data: FormValues) => login(data);

  return (
    <div style={styles.container}>
      <div style={styles.card}>
        <h1 style={styles.title}>Products API</h1>
        <p style={styles.subtitle}>Sign in to manage products</p>

        <form onSubmit={handleSubmit(onSubmit)} style={styles.form}>
          <div style={styles.field}>
            <label style={styles.label}>Username</label>
            <input
              {...register('username')}
              placeholder="admin"
              style={styles.input}
              autoComplete="username"
            />
            {errors.username && <span style={styles.error}>{errors.username.message}</span>}
          </div>

          <div style={styles.field}>
            <label style={styles.label}>Password</label>
            <input
              {...register('password')}
              type="password"
              placeholder="password"
              style={styles.input}
              autoComplete="current-password"
            />
            {errors.password && <span style={styles.error}>{errors.password.message}</span>}
          </div>

          {isError && (
            <div style={styles.errorBox}>
              {(error as any)?.response?.data?.message ?? 'Login failed. Please try again.'}
            </div>
          )}

          <button type="submit" disabled={isPending} style={styles.button}>
            {isPending ? 'Signing in...' : 'Sign In'}
          </button>
        </form>

        <p style={styles.hint}>Test credentials: <strong>admin</strong> / <strong>password</strong></p>
      </div>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  container: { minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', background: '#f0f4f8' },
  card: { background: '#fff', borderRadius: 12, padding: '2.5rem', width: 360, boxShadow: '0 4px 24px rgba(0,0,0,0.1)' },
  title: { margin: 0, fontSize: '1.75rem', fontWeight: 700, color: '#1a202c' },
  subtitle: { marginTop: 4, marginBottom: '1.5rem', color: '#718096', fontSize: '0.95rem' },
  form: { display: 'flex', flexDirection: 'column', gap: '1rem' },
  field: { display: 'flex', flexDirection: 'column', gap: 4 },
  label: { fontSize: '0.875rem', fontWeight: 600, color: '#4a5568' },
  input: { padding: '0.625rem 0.75rem', border: '1px solid #e2e8f0', borderRadius: 6, fontSize: '1rem', outline: 'none' },
  error: { fontSize: '0.75rem', color: '#e53e3e' },
  errorBox: { background: '#fff5f5', border: '1px solid #fed7d7', borderRadius: 6, padding: '0.5rem 0.75rem', color: '#c53030', fontSize: '0.875rem' },
  button: { padding: '0.75rem', background: '#3182ce', color: '#fff', border: 'none', borderRadius: 6, fontWeight: 600, fontSize: '1rem', cursor: 'pointer' },
  hint: { marginTop: '1.5rem', fontSize: '0.8rem', color: '#a0aec0', textAlign: 'center' },
};
