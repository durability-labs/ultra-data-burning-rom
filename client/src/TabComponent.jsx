import React from 'react';

export default function TabComponent() {
  const [activeTab, setActiveTab] = React.useState(0);
  const tabs = ["Upload", "Catalogue", "Download"];
  return (
    <div style={{display: 'flex', flexDirection: 'column', height: '100%', width: '100%'}}>
      <div style={{
        display: 'flex',
        borderBottom: '1px solid #ccc',
        background: '#f7f7f7',
        height: '34px'
      }}>
        {tabs.map((tab, idx) => (
          <button
            key={tab}
            onClick={() => setActiveTab(idx)}
            style={{
              flex: 1,
              padding: '0.5rem 0',
              background: activeTab === idx ? '#fff' : '#f7f7f7',
              border: 'none',
              borderBottom: activeTab === idx ? '2px solid #1976d2' : '2px solid transparent',
              color: activeTab === idx ? '#1976d2' : '#333',
              fontWeight: activeTab === idx ? 'bold' : 'normal',
              fontSize: '1rem',
              cursor: 'pointer',
              outline: 'none',
              transition: 'background 0.2s'
            }}
          >
            {tab}
          </button>
        ))}
      </div>
      <div style={{flex: 1, padding: '2rem', background: '#fff', overflow: 'auto'}}>
        {activeTab === 0 && <div>Upload content goes here.</div>}
        {activeTab === 1 && <div>Catalogue content goes here.</div>}
        {activeTab === 2 && <div>Download content goes here.</div>}
      </div>
    </div>
  );
}
