import apiClient from './client';
import type { CreateProductRequest, ProductDto } from '../types';

export async function getProducts(): Promise<ProductDto[]> {
  const response = await apiClient.get<ProductDto[]>('/products');
  return response.data;
}

export async function getProductsByColour(colour: string): Promise<ProductDto[]> {
  const response = await apiClient.get<ProductDto[]>(`/products/by-colour/${colour}`);
  return response.data;
}

export async function createProduct(data: CreateProductRequest): Promise<ProductDto> {
  const response = await apiClient.post<ProductDto>('/products', data);
  return response.data;
}
