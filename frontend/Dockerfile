# Stage 1 — Build
FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

# VITE_API_URL must be provided at build time — Vite bakes it into the bundle
ARG VITE_API_URL=http://localhost:5000/api
ENV VITE_API_URL=$VITE_API_URL

COPY . .
RUN npm run build

# Stage 2 — Serve with nginx
FROM nginx:alpine AS final
# SPA fallback: all routes serve index.html so React Router handles them
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
