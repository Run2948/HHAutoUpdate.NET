FROM node:14.7.0-slim

EXPOSE 9000

RUN mkdir /app
WORKDIR /app
COPY . /app

RUN npm config set registry http://registry.npm.taobao.org
RUN npm install

RUN npm run build
WORKDIR /app/dist

ENTRYPOINT ["node", "app.js"]