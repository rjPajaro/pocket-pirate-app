'use strict';

const { contextBridge, ipcRenderer } = require('electron');

// Expose a safe update API to the renderer (contextIsolation: true)
contextBridge.exposeInMainWorld('updater', {
  onUpdateAvailable:  (cb) => ipcRenderer.on('update-available',  (_, info) => cb(info)),
  onDownloadProgress: (cb) => ipcRenderer.on('download-progress', (_, p)    => cb(p)),
  onUpdateDownloaded: (cb) => ipcRenderer.on('update-downloaded', (_, info) => cb(info)),
  installUpdate: () => ipcRenderer.send('install-update'),
});
