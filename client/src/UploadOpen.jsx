import React, { useState, useMemo, useEffect } from 'react';
import UploadButton from './UploadButton';
import { useAppContext } from './AppContext';
import BurnButton from './BurnButton';
import formatBytes from './format';

export default function UploadOpen(username, bucket, bytesUsed) {
  function handleDelete(file) {
    if (!username) return;
    const confirmed = window.confirm(`Delete file "${file.filename}"?`);
    if (!confirmed) return;
    // Assume file has an id property, fallback to filename if not
    const fileId = file.id || file.filename;
    fetch(`/bucket/${username}/${fileId}`, { method: 'DELETE' })
      .then(res => {
        updateBucket(username);
      })
      .catch(() => window.alert('Failed to delete file.'));
  }

  return (bucket && (
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
            {bucket.entries.map((file, idx) => (
              <tr key={idx}>
                <td style={{ padding: '8px', borderBottom: '1px solid #eee' }}>{file.filename}</td>
                <td style={{ padding: '8px', borderBottom: '1px solid #eee' }}>
                  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                    <span>{formatBytes(file.byteSize)}</span>
                    <button
                      title="Delete file"
                      onClick={() => handleDelete(file)}
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
            Storage used: {((bytesUsed / 1048576).toFixed(2))} MB / {((bucket.volumeSize / 1048576).toFixed(2))} MB
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
              width: `${Math.min(100, (bytesUsed / bucket.volumeSize) * 100)}%`,
              height: '100%',
              background: '#1976d2',
              transition: 'width 0.3s'
            }} />
          </div>
          <div style={{ marginBottom: '0.5rem', color: '#fff', fontSize: '0.75rem' }}>
            Burn ROM before: {new Date(bucket.expiryUtc).toLocaleString()}
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
  ));
}
