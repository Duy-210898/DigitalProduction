// ecosystem.config.js
module.exports = {
  apps: [{
    name: "cutting-server",
    script: "server.js",
    cwd: "Z:/CuttingProjectServer/server", // Or local path like "C:/CuttingProjectServer/server"
    watch: true,
    env: {
      NODE_ENV: "production"
    }
  }]
};
