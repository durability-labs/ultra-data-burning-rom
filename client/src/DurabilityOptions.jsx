import React from 'react';

export default function DurabilityOptions(durabilityInfo, selectedId, setSelectedId) {
  return (
    <div style={{ border: '2px solid #1976d2', borderRadius: '8px', padding: '1rem', background: '#181818ff', maxWidth: '600px', margin: '0 auto', marginTop: '1.5rem' }}>
      {!durabilityInfo && (
        <div style={{ color: '#fff' }}>Loading durability options...</div>
      )}
      {durabilityInfo && (
        <>
          <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'center', flexWrap: 'wrap' }}>
            {durabilityInfo.options.map(opt => (
                <button
                  key={opt.id}
                  onClick={() => setSelectedId(opt.id)}
                  style={{
                    padding: '0.5rem 0.75rem',
                    borderRadius: '6px',
                    background: selectedId === opt.id ? '#1976d2' : '#2a2a2a',
                    color: '#fff',
                    border: selectedId === opt.id ? '2px solid #1976d2' : '1px solid #444',
                    cursor: 'pointer'
                  }}
                >
                  <div style={{ marginTop: '1rem', color: '#fff', textAlign: 'left' }}>
                    <div style={{ fontWeight: 'bold', fontSize: '1.1rem' }}>{opt.name}</div>
                    {opt.priceLine && <div style={{ marginTop: '0.25rem', color: '#cfe8ff' }}>{opt.priceLine}</div>}
                    {opt.description && <div style={{ marginTop: '0.5rem' }}>{opt.description}</div>}
                    {opt.sponsorLine && <div style={{ marginTop: '0.5rem', color: '#9ec9ff' }}>{opt.sponsorLine}</div>}
                  </div>
                </button>
              )
          )}
          </div>
        </>
      )}
    </div>
  )
}
