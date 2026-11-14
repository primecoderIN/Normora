# Build stage
FROM node:22 AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci --legacy-peer-deps
COPY . .
RUN if [ ! -f src/environments/environment.ts ]; then cp src/environments/environment.template.ts src/environments/environment.ts; fi
RUN if [ ! -f src/environments/environment.prod.ts ]; then cp src/environments/environment.template.ts src/environments/environment.prod.ts; fi
RUN npm run build -- --configuration production

# Serve stage
FROM nginx:alpine
RUN apk add --update nodejs npm
WORKDIR /app
COPY --from=build /app/dist /app/dist
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY start.sh /app/start.sh
RUN chmod +x /app/start.sh
EXPOSE 80
CMD ["/app/start.sh"]
