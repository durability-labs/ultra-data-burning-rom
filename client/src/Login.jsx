import React, { useState } from 'react';

export default function LoginDialog({ onAccept }) {
  const [acknowledged, setAcknowledged] = useState(false);
  const [inputUsername, setInputUsername] = useState("");
  const [open, setOpen] = useState(true);

  const handleAccept = () => {
    onAccept(inputUsername);
    setOpen(false);
  };

  if (!open) return null;

  return (
    <div style={{
      position: 'fixed',
      top: 0,
      left: 0,
      width: '100vw',
      height: '100vh',
      background: 'rgba(201, 201, 201, 1)',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      zIndex: 1000
    }}>
      <div style={{
        background: '#363636ff',
        color: 'rgba(238, 238, 238, 1)',
        padding: '2rem',
        borderRadius: '8px',
        minWidth: '320px',
        boxShadow: '0 2px 16px rgba(0,0,0,0.2)'
      }}>
        <h2>Disclaimer</h2>
        <p style={{textAlign: 'left'}}>This application is for demonstration purposes only. By proceeding, you acknowledge that you have read and understood this disclaimer.</p>
        <div style={{margin: '1rem 0'}}>
          <label>
            <input
              type="checkbox"
              checked={acknowledged}
              onChange={e => setAcknowledged(e.target.checked)}
            />{' '}
            I acknowledge the disclaimer
          </label>
        </div>
        <div style={{margin: '1rem 0'}}>
          <input
            type="text"
            placeholder="Enter your username"
            value={inputUsername}
            onChange={e => setInputUsername(e.target.value)}
            style={{width: '100%', padding: '0.5rem'}}
          />
        </div>
        <button
          onClick={handleAccept}
          disabled={!acknowledged || !inputUsername.trim()}
          style={{padding: '0.5rem 1.5rem'}}
        >
          Accept
        </button>
      </div>
    </div>
  );
}
