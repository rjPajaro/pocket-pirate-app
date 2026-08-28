'use strict';

const { app, BrowserWindow } = require('electron');
const { spawn } = require('child_process');
const net = require('net');
const path = require('path');

const API_PORT = 5000;
let apiProcess = null;

function findFreePort(preferred) {
  return new Promise((resolve) => {
    const server = net.createServer();
    server.listen(preferred, '127.0.0.1', () => {
      const port = server.address().port;
      server.close(() => resolve(port));
    });
    server.on('error', () => {
      // preferred port taken — let OS pick one
      const fallback = net.createServer();
      fallback.listen(0, '127.0.0.1', () => {
        const port = fallback.address().port;
        fallback.close(() => resolve(port));
      });
    });
  });
}

function waitForApi(port, retries = 20) {
  return new Promise((resolve, reject) => {
    const attempt = () => {
      const sock = net.createConnection({ port, host: '127.0.0.1' });
      sock.on('connect', () => { sock.destroy(); resolve(); });
      sock.on('error', () => {
        if (--retries <= 0) return reject(new Error('API did not start in time'));
        setTimeout(attempt, 500);
      });
    };
    attempt();
  });
}

app.whenReady().then(async () => {
  // Show splash immediately while API boots
  const splash = new BrowserWindow({
    width: 380,
    height: 240,
    frame: false,
    resizable: false,
    center: true,
    skipTaskbar: true,
    webPreferences: { nodeIntegration: false },
  });
  // In packaged app, extraFiles land next to the exe; in dev, use __dirname
  const splashPath = app.isPackaged
    ? path.join(path.dirname(process.execPath), 'splash.html')
    : path.join(__dirname, 'splash.html');
  splash.loadFile(splashPath);

  const port = await findFreePort(API_PORT);

  const apiDir = path.join(process.resourcesPath, 'api');
  const apiExe = path.join(apiDir, 'PocketPirate.Api.exe');

  apiProcess = spawn(apiExe, [], {
    cwd: apiDir,
    env: {
      ...process.env,
      ASPNETCORE_URLS: `http://127.0.0.1:${port}`,
      ASPNETCORE_ENVIRONMENT: 'Production',
    },
    stdio: 'ignore',
  });

  apiProcess.on('error', (err) => {
    console.error('Failed to start API process:', err);
  });

  try {
    await waitForApi(port);
  } catch (e) {
    console.error(e.message);
  }

  const win = new BrowserWindow({
    width: 900,
    height: 700,
    show: false,
    title: 'Pocket Pirate v0.1.0',
    webPreferences: { nodeIntegration: false, contextIsolation: true },
  });

  const showMain = () => {
    if (!splash.isDestroyed()) splash.destroy();
    if (!win.isVisible()) win.show();
  };

  win.once('ready-to-show', showMain);
  // Fallback in case ready-to-show doesn't fire
  setTimeout(showMain, 10000);

  // Keep all navigation inside the Electron window — never open system browser
  win.webContents.setWindowOpenHandler(() => ({ action: 'deny' }));
  win.webContents.on('will-navigate', (event, url) => {
    if (!url.startsWith('file://')) event.preventDefault();
  });

  const indexPath = path.join(process.resourcesPath, 'app', 'index.html');
  win.loadFile(indexPath, { query: { apiPort: String(port) } });

  win.webContents.on('did-finish-load', () => {
    win.webContents.executeJavaScript(`window.__API_PORT__ = ${port};`);
  });
});

app.on('will-quit', () => {
  if (apiProcess) apiProcess.kill();
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit();
});
