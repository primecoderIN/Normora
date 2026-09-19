#!/bin/sh
# Start the Node.js SSR server in the background
node /app/dist/client/server/server.mjs &

# Start Nginx in the foreground
nginx -g "daemon off;"
