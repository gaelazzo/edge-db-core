const https = require('https');
const fs = require('fs');
const path = require('path');

const downloadFile = async (url, dest) => {
  //console.log(`downloadFile: ${url} nella cartella: ${dest}`);
  const response = await new Promise((resolve, reject) => {
    https.get(url, resolve).on('error', reject);
  });

  await new Promise((resolve, reject) => {
    const file = fs.createWriteStream(dest);
    response.pipe(file);
    file.on('finish', resolve);
    file.on('error', reject);
  });

  //console.log(`File downloaded: ${dest}`);
};

const downloadDirectory = async (url, destDir) => {
  try {
    const response = await new Promise((resolve, reject) => {
      const options = {
        headers: {
          'User-Agent': 'YourUserAgent' // Sostituisci 'YourUserAgent' con un'identificazione appropriata per la tua applicazione
        }
      };
      https.get(url, options, resolve).on('error', reject);
    });
    const files = JSON.parse(await getResponseData(response));

    for (const file of files) {
      const filePath = path.join(destDir, file.name);

      if (file.type === 'file') {
        //console.log(`downloadFile il file: ${filePath}`);
        await downloadFile(file.download_url, filePath);
      } else if (file.type === 'dir') {
        //console.log(`download folder: ${filePath}`);
        await fs.promises.mkdir(filePath, { recursive: true });
        await downloadDirectory(file.url, filePath);
      }
    }
  } catch (error) {
    console.error('Errore durante il download:', error);
    throw error;
  }
};

const getResponseData = async (response) => {
  return new Promise((resolve, reject) => {
    let data = '';
    response.on('data', chunk => data += chunk);
    response.on('end', () => resolve(data));
    response.on('error', reject);
  });
};

const platform = process.platform; // Ottieni la piattaforma corrente (darwin, win32, linux)
const baseUrl = `https://api.github.com/repos/gaelazzo/edge-db-core/contents/lib/${platform}`;
const destDir = path.join(__dirname, '../edge-db-core/lib', platform);

fs.promises.mkdir(destDir, { recursive: true })
.then(() => downloadDirectory(baseUrl, destDir))
.then(() => console.log('Downloaded platform:'+platform))
.catch(error => console.error('Error downloading platform ', platform+":",error));
