import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { createProduct, getProducts, getProductsByColour } from '../api/products';
import type { CreateProductRequest } from '../types';

export function useProducts() {
  return useQuery({
    queryKey: ['products'],
    queryFn: getProducts,
  });
}

export function useProductsByColour(colour: string) {
  return useQuery({
    queryKey: ['products', 'colour', colour],
    queryFn: () => getProductsByColour(colour),
    enabled: !!colour,
  });
}

export function useCreateProduct() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateProductRequest) => createProduct(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['products'] });
    },
  });
}
