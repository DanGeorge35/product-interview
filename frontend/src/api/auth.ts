import apiClient from './client';

export async function login(username: string, password: string): Promise<string> {
  const response = await apiClient.post<{ token: string }>('/auth/token', { username, password });
  return response.data.token;
}
