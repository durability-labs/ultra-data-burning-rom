import React, { useState, useEffect } from 'react';
import UploadButton from './UploadButton';
import { useAppContext } from './AppContext';
import BurnButton from './BurnButton';

export default function Upload() {
  const { username } = useAppContext();
  const [files, setFiles] = useState([]);
  const [storage, setStorage] = useState({ bytesUsed: 0, totalBytes: 734003200 });

  useEffect(() => {
    if (!username) return;
    fetch(`/bucket/${username}`)
      .then(res => res.json())
      .then(data => {
        var used = 0
        var files = []
        for (const entry of data.entries) {
          used += entry.byteSize;
          files.push({
            id: entry.id,
            filename: entry.filename,
            size: formatBytes(entry.byteSize)
          })
        }
        setFiles(files);
        setStorage({ bytesUsed: used, totalBytes: data.volumeSize });
      })
      .catch(() => {
        setFiles([]);
        setStorage({ bytesUsed: 0, totalBytes: 1 });
      });
  }, [username]);

  function formatBytes(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  return (
    <>
      <div style={{ border: '2px solid #1976d2', borderRadius: '8px', padding: '1rem', background: '#181818ff', maxWidth: '600px', margin: '0 auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr>
              <th style={{ borderBottom: '1px solid #ccc', textAlign: 'left', padding: '8px' }}>Filename</th>
              <th style={{ borderBottom: '1px solid #ccc', textAlign: 'left', padding: '8px' }}>Size</th>
            </tr>
          </thead>
          <tbody>
            {files.map((file, idx) => (
              <tr key={idx}>
                <td style={{ padding: '8px', borderBottom: '1px solid #eee' }}>{file.filename}</td>
                <td style={{ padding: '8px', borderBottom: '1px solid #eee' }}>
                  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                    <span>{file.size}</span>
                    <button
                      title="Delete file"
                      onClick={() => window.alert('Delete!')}
                      style={{
                        background: 'none',
                        border: 'none',
                        padding: '0 0 0 8px',
                        cursor: 'pointer',
                        display: 'inline-flex',
                        alignItems: 'center'
                      }}
                    >
                      <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#d32f2f" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m5 0V4a2 2 0 0 1 2-2h0a2 2 0 0 1 2 2v2"/><line x1="10" y1="11" x2="10" y2="17"/><line x1="14" y1="11" x2="14" y2="17"/></svg>
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        <div style={{ marginTop: '1.5rem' }}>
          <div style={{ marginBottom: '0.5rem', color: '#fff', fontSize: '0.95rem' }}>
            Storage used: {((storage.bytesUsed / 1048576).toFixed(2))} MB / {((storage.totalBytes / 1048576).toFixed(2))} MB
          </div>
          <div style={{
            width: '100%',
            height: '18px',
            background: '#333',
            borderRadius: '8px',
            overflow: 'hidden',
            boxShadow: '0 1px 4px rgba(0,0,0,0.08)'
          }}>
            <div style={{
              width: `${Math.min(100, (storage.bytesUsed / storage.totalBytes) * 100)}%`,
              height: '100%',
              background: '#1976d2',
              transition: 'width 0.3s'
            }} />
          </div>
        </div>
      </div>
      <div style={{ maxWidth: '600px', margin: '1.5rem auto 1.5rem auto', textAlign: 'center' }}>
        <UploadButton />
      </div>
      <div style={{ maxWidth: '600px', margin: '1.5rem auto 1.5rem auto', textAlign: 'center' }}>
        <BurnButton />
      </div>
    </>
  );
}
