export interface ProductDto {
  id: string;
  name: string;
  description: string | null;
  colour: string;
  price: number;
  stockQuantity: number;
  createdAt: string;
}

export interface CreateProductRequest {
  name: string;
  description?: string;
  colour: string;
  price: number;
  stockQuantity: number;
}

export interface ApiErrorResponse {
  title: string;
  errors?: string[];
}

export const COLOURS = [
  'Red', 'Blue', 'Green', 'Yellow', 'Black',
  'White', 'Orange', 'Purple', 'Pink', 'Brown', 'Grey',
] as const;

export type Colour = typeof COLOURS[number];
