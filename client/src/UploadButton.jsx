import React, { useState } from 'react';
import { useAppContext } from './AppContext';

export default function UploadButton() {
  const { username } = useAppContext();

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [uploadProgress, setUploadProgress] = useState(0);
  const [uploadStatus, setUploadStatus] = useState('idle'); // 'idle' | 'uploading' | 'success' | 'error'
  const [uploadFileName, setUploadFileName] = useState('');
  const [uploadError, setUploadError] = useState('');

  const resetUploadState = () => {
    setIsModalOpen(false);
    setUploadProgress(0);
    setUploadStatus('idle');
    setUploadFileName('');
    setUploadError('');
  };

  const handleFileChange = (event) => {
    const file = event.target.files[0];
    if (!file || !username) return;

    const formData = new FormData();
    formData.append('file', file);

    // Open modal and initialize state
    setIsModalOpen(true);
    setUploadStatus('uploading');
    setUploadProgress(0);
    setUploadFileName(file.name);
    setUploadError('');

    const xhr = new XMLHttpRequest();

    xhr.upload.onprogress = (e) => {
      if (e.lengthComputable) {
        const percent = Math.round((e.loaded / e.total) * 100);
        setUploadProgress(percent);
      }
    };

    xhr.onload = () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        setUploadProgress(100);
        setUploadStatus('success');
      } else {
        setUploadStatus('error');
        setUploadError('Upload failed.');
      }
    };

    xhr.onerror = () => {
      setUploadStatus('error');
      setUploadError('Upload failed.');
    };

    xhr.open('POST', `/bucket/${username}`);
    xhr.send(formData);
  };

  const handleClick = () => {
    document.getElementById('upload-input').click();
  };

  return (
    <>
      <input
        id="upload-input"
        type="file"
        style={{ display: 'none' }}
        onChange={handleFileChange}
      />
      <button
        onClick={handleClick}
        style={{
          padding: '0.5rem 2rem',
          fontSize: '1rem',
          background: '#1976d2',
          color: '#fff',
          border: 'none',
          borderRadius: '6px',
          cursor: 'pointer',
          boxShadow: '0 1px 4px rgba(0,0,0,0.08)'
        }}
      >
        Upload
      </button>

      {isModalOpen && (
        <div
          style={{
            position: 'fixed',
            inset: 0,
            backgroundColor: 'rgba(0, 0, 0, 0.4)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 1000
          }}
        >
          <div
            role="dialog"
            aria-modal="true"
            style={{
              minWidth: '280px',
              maxWidth: '400px',
              background: '#eee',
              color: '#000',
              borderRadius: '8px',
              padding: '1rem 1.25rem',
              boxShadow: '0 8px 24px rgba(0,0,0,0.18)'
            }}
          >
            {uploadStatus === 'uploading' && (
              <div>
                <div style={{ marginBottom: '0.5rem', fontWeight: 500 }}>
                  Uploading {uploadFileName || 'file'}...
                </div>
                <div
                  style={{
                    width: '100%',
                    height: '10px',
                    borderRadius: '999px',
                    background: '#e0e0e0',
                    overflow: 'hidden',
                    margin: '0.5rem 0 0.25rem'
                  }}
                >
                  <div
                    style={{
                      width: `${uploadProgress}%`,
                      height: '100%',
                      background: '#1976d2',
                      transition: 'width 0.2s ease'
                    }}
                  />
                </div>
                <div style={{ fontSize: '0.85rem', color: '#555' }}>
                  {uploadProgress}%
                </div>
              </div>
            )}

            {uploadStatus === 'success' && (
              <div>
                <div style={{ marginBottom: '0.5rem', fontWeight: 500 }}>
                  Upload complete
                </div>
                <div style={{ fontSize: '0.9rem', marginBottom: '1rem' }}>
                  {uploadFileName} has been uploaded successfully.
                </div>
                <button
                  onClick={resetUploadState}
                  style={{
                    padding: '0.35rem 1.25rem',
                    fontSize: '0.9rem',
                    background: '#1976d2',
                    color: '#fff',
                    border: 'none',
                    borderRadius: '4px',
                    cursor: 'pointer'
                  }}
                >
                  Close
                </button>
              </div>
            )}

            {uploadStatus === 'error' && (
              <div>
                <div style={{ marginBottom: '0.5rem', fontWeight: 500, color: '#d32f2f' }}>
                  Upload failed
                </div>
                <div style={{ fontSize: '0.9rem', marginBottom: '1rem' }}>
                  {uploadError || 'Something went wrong during the upload.'}
                </div>
                <button
                  onClick={resetUploadState}
                  style={{
                    padding: '0.35rem 1.25rem',
                    fontSize: '0.9rem',
                    background: '#d32f2f',
                    color: '#fff',
                    border: 'none',
                    borderRadius: '4px',
                    cursor: 'pointer'
                  }}
                >
                  Close
                </button>
              </div>
            )}
          </div>
        </div>
      )}
    </>
  );
}
