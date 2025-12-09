import React, { useState, useEffect } from 'react';
import { useAppContext } from './AppContext';
import EditableFieldsTable from './EditableFieldsTable';
import DurabilityOptions from './DurabilityOptions';

const defaultDurabilityInfo = {
    options: [
      {
        id: 0,
        name: '',
        priceLine: '',
        description: '',
        sponsorLine: ''
      }
    ]
  };

export default function BurnDialog({ open, onClose }) {
  const { username } = useAppContext();
    const [fields, setFields] = useState({
    title: '',
    author: '',
    tags: '',
    description: ''
  });
  const [selectedId, setSelectedId] = useState(0);
  const [durabilityInfo, setDurabilityInfo] = useState(defaultDurabilityInfo);

  useEffect(() => {
    fetch('/durability')
      .then(res => res.json())
      .then(data => {
        setDurabilityInfo(data);
        setSelectedId(data.options[0]?.id ?? null);
      })
      .catch(() => {
        setDurabilityInfo(defaultDurabilityInfo);
      });
  }, []);

  async function handleBurn() {
    try {
      const res = await fetch(`/bucket/${encodeURIComponent(username)}/burnrom`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          fields: fields,
          durabilityOptionId: selectedId
        })
      });
      if (!res.ok) {
        console.error('Burn request failed:', res.status, res.statusText);
      }
    } catch (err) {
      console.error('Error sending burn request:', err);
    } finally {
      onClose();
    }
  }
  if (!open) return null;
  return (
    <div style={{
      position: 'fixed',
      top: 0,
      left: 0,
      width: '100vw',
      height: '100vh',
      background: 'rgba(0,0,0,0.5)',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      zIndex: 2000
    }}>
      <div style={{
        background: '#4e4e4eff',
        borderRadius: '10px',
        padding: '2rem 2.5rem',
        minWidth: '420px',
        boxShadow: '0 2px 16px rgba(0,0,0,0.2)',
        textAlign: 'center'
      }}>
        <h2 style={{marginTop: 0}}>Finalize Burn</h2>
        {EditableFieldsTable(fields, setFields)}
        {DurabilityOptions(durabilityInfo, selectedId, setSelectedId)}
        <button
          style={{
            margin: '2rem',
            padding: '0.5rem 2rem',
            fontSize: '1rem',
            background: '#1976d2',
            color: '#fff',
            border: 'none',
            borderRadius: '6px',
            cursor: 'pointer',
            boxShadow: '0 1px 4px rgba(0,0,0,0.08)'
          }}
          onClick={handleBurn}
        >
          Burn!
        </button>
        <button
          style={{
            margin: '2rem',
            padding: '0.5rem 2rem',
            fontSize: '1rem',
            background: '#1976d2',
            color: '#fff',
            border: 'none',
            borderRadius: '6px',
            cursor: 'pointer',
            boxShadow: '0 1px 4px rgba(0,0,0,0.08)'
          }}
          onClick={onClose}
        >
          Cancel
        </button>
      </div>
    </div>
  );
}
