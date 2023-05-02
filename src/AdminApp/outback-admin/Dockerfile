#stage 1
FROM node:16.14.0 as node
WORKDIR /app
COPY . .
RUN npm install
RUN node_modules/.bin/ng build --base-href /admin --deploy-url /admin/
#stage 2
FROM nginx:alpine
COPY --from=node /app/dist/outback-admin /usr/share/nginx/html
