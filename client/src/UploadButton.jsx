import React from 'react';
import { useAppContext } from './AppContext';

export default function UploadButton() {
  const { username } = useAppContext();
  const handleFileChange = (event) => {
    const file = event.target.files[0];
    if (file && username) {
      const formData = new FormData();
      formData.append('file', file);
      fetch(`/bucket/${username}`, {
        method: 'POST',
        body: formData
      })
        .then(res => {
          if (res.ok) {
            window.alert('File uploaded: ' + file.name);
          } else {
            window.alert('Upload failed.');
          }
        })
        .catch(() => window.alert('Upload failed.'));
    }
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
    </>
  );
}
