import React from 'react';
import Upload from './Upload';
import Catalogue from './Catalogue';
import Download from './Download';
import { useAppContext } from './AppContext';

export default function TabComponent() {
  const { activeTab, setActiveTab } = useAppContext();
  const tabs = ["Upload and Burn", "Browse the Catalogue", "Mount and Download"];
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
      <div style={{flex: 1, padding: '2rem', background: '#424242ff', overflow: 'auto'}}>
        {activeTab === 0 && <Upload />}
        {activeTab === 1 && <Catalogue />}
        {activeTab === 2 && <Download />}
      </div>
    </div>
  );
}
