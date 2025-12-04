import React from 'react';

export default function UploadButton() {
  const handleFileChange = (event) => {
    const file = event.target.files[0];
    if (file) {
      // Placeholder: upload file to backend
      console.log('Selected file:', file);
      window.alert('File selected: ' + file.name);
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
