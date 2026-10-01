import { useMutation } from '@tanstack/react-query';
import { login } from '../api/auth';
import { useAuthStore } from '../store/authStore';

export function useLogin() {
  const setToken = useAuthStore((s) => s.setToken);

  return useMutation({
    mutationFn: ({ username, password }: { username: string; password: string }) =>
      login(username, password),
    onSuccess: (token) => setToken(token),
  });
}
