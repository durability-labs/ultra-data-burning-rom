import React from 'react';
import { useAppContext } from './AppContext';
import TabComponent from './TabComponent';

export default function Home() {
  const { username, clearUsername } = useAppContext();
  return (
    <>
      <div style={{
        width: '100vw',
        position: 'fixed',
        top: 0,
        left: 0,
        height: '36px',
        background: '#222',
        color: '#fff',
        display: 'flex',
        alignItems: 'center',
        paddingLeft: '1.5rem',
        fontSize: '1rem',
        zIndex: 100
      }}>
        <span>{username}</span>
        <button
          title="logout"
          onClick={clearUsername}
          style={{
            marginLeft: '1rem',
            padding: '2px 10px',
            fontSize: '0.9rem',
            background: '#444',
            color: '#fff',
            border: 'none',
            borderRadius: '4px',
            cursor: 'pointer',
            height: '24px'
          }}
        >
          Logout
        </button>
      </div>
      <div style={{
        height: 'calc(100vh - 48px)',
        marginTop: '24px',
        width: 'calc(100vw)',
        display: 'flex'
      }}>
        <TabComponent />
      </div>
    </>
  );
}
