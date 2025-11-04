# Stage 1: Build the Angular application
FROM node:20-alpine AS build

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source files
COPY . .

# Build the application for production
RUN npm run build -- --configuration production

# Stage 2: Serve with Nginx
FROM nginx:alpine

# Copy custom nginx configuration template
COPY nginx.clean.conf /etc/nginx/nginx.conf.template

# Copy built application from build stage
COPY --from=build /app/dist/desafio-by-coders.ui.web/browser /usr/share/nginx/html

# Expose port 80
EXPOSE 80

# Create entrypoint script to substitute environment variables
RUN echo '#!/bin/sh' > /docker-entrypoint.sh && \
    echo 'set -e' >> /docker-entrypoint.sh && \
    echo 'echo "Configuring nginx with API_URL=${API_URL:-http://localhost:8080}"' >> /docker-entrypoint.sh && \
    echo 'envsubst "\$API_URL" < /etc/nginx/nginx.conf.template > /etc/nginx/nginx.conf' >> /docker-entrypoint.sh && \
    echo 'exec nginx -g "daemon off;"' >> /docker-entrypoint.sh && \
    chmod +x /docker-entrypoint.sh

# Set default API_URL if not provided
ENV API_URL=http://localhost:8080

# Start nginx with environment variable substitution
ENTRYPOINT ["/docker-entrypoint.sh"]
