const https = require('https');
const fs = require('fs');
const path = require('path');
const { promisify } = require('util');

const downloadFile = promisify((url, dest, callback) => {
  const file = fs.createWriteStream(dest);
  https.get(url, (response) => {
    response.pipe(file);
    file.on('finish', () => {
      file.close(callback);
    });
  }).on('error', (err) => {
    fs.unlink(dest, () => {
      if (callback) callback(err);
    });
  });
});

const downloadDirectory = async (url, destDir) => {
  const response = await fetch(url);
  const files = await response.json();
  await Promise.all(files.map(async (file) => {
    const filePath = path.join(destDir, file.name);
    if (file.type === 'file') {
      await downloadFile(file.download_url, filePath);
    } else if (file.type === 'dir') {
      await fs.promises.mkdir(filePath, { recursive: true });
      await downloadDirectory(file.url, filePath);
    }
  }));
};

const platform = process.platform; // Ottieni la piattaforma corrente (darwin, win32, linux)
const baseUrl = `https://api.github.com/repos/gaelazzo/edge-db-core/contents/lib/${platform}`;

const destDir = path.join(__dirname, '../node_modules/edge-db-core/lib', platform);
fs.mkdirSync(destDir, { recursive: true }); // Crea la directory di destinazione

downloadDirectory(baseUrl, destDir)
  .then(() => console.log('Download completato'))
  .catch((error) => console.error('Errore durante il download:', error));
