import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useAuthStore } from './store/authStore';
import LoginPage from './pages/LoginPage';
import ProductsPage from './pages/ProductsPage';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 30_000,
    },
  },
});

export default function App() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated());

  return (
    <QueryClientProvider client={queryClient}>
      {isAuthenticated ? <ProductsPage /> : <LoginPage />}
    </QueryClientProvider>
  );
}
