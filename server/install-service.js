const Service = require('node-windows').Service;
const path = require('path');

// Create a new service object
const svc = new Service({
  name: 'CuttingProjectServer',
  description: 'Node.js server for Cutting Project running as a Windows service',
  script: path.join(__dirname, 'cutting-server.js'), // path to your server.js
  nodeOptions: [
    '--harmony',
    '--max_old_space_size=4096'
  ],
// Optional: Run as specific user (if needed)
//   user: {
//     domain: 'your-domain',
//     account: 'your-username',
//     password: 'your-password'
//   }
});

// Listen for install event
svc.on('install', () => {
  console.log('✅ Service installed successfully.');
  svc.start();
});

// Install the service
svc.install();
