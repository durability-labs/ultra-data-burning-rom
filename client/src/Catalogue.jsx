import React, {useEffect, useState} from 'react';
import { useAppContext } from './AppContext';

export default function Catalogue() {
  const { popularInfo, setPopularInfo, searchResult, setSearchResult, setRomCid, setActiveTab } = useAppContext();
  const [query, setQuery] = useState('');

  useEffect(() => {
    if (popularInfo.roms.length === 0) {
      fetch('/catalogue')
        .then(res => res.json())
        .then(data => {
          setPopularInfo(data);
        })
        .catch(() => {
        });
    }
  }, []);

  function setPopularRoms() {
    fetch('/catalogue')
      .then(res => res.json())
      .then(data => {
        setPopularInfo(data);
        setSearchResult(popularInfo);
      })
      .catch(() => {
      });
  }

  function runSearch(value) {
    const trimmed = value.trim();
    if (!trimmed) {
      return;
    }
    setQuery(value);
    fetch(`/catalogue/search/${encodeURIComponent(trimmed)}`, { method: 'POST' })
      .then(res => res.json())
      .then(data => {
        setSearchResult(data);
      })
      .catch(() => {
        // ignore errors; leave previous results
      });
  }

  function handleOpenRom(romCid) {
    setRomCid(romCid);
    setActiveTab(2);
  }

  return <>
    <div style={{ border: '2px solid #1976d2', borderRadius: '8px', padding: '1rem', margin: '1rem', background: '#181818ff', maxWidth: '600px', margin: '0 auto' }}>
      <div style={{ borderBottom: '1px solid #ccc', textAlign: 'left', padding: '8px' }}>Search</div>
      <input
        type="text"
        placeholder="Search the catalogue..."
        value={query}
        style={{ width: '100%', boxSizing: 'border-box', padding: '0.5rem', marginTop: '0.5rem', marginBottom: '0.5rem', borderRadius: '6px', border: '1px solid #444', background: '#0f0f0f', color: '#fff' }}
        onChange={e => {
          const value = e.target.value;
          setQuery(value);
        }}
        onKeyDown={e => {
          if (e.key !== 'Enter') return;
          runSearch(query);
        }}
      />
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', flexWrap: 'wrap' }}>
        <button style={{
                padding: '0.5rem 0.75rem',
                background: '#1976d2',
                color: '#fff',
                border: 'none',
                borderRadius: '6px',
                cursor: 'pointer'
            }}
            onClick={() => setPopularRoms()}
            >Popular ROMs</button>
        <div style={{ color: '#ccc', fontWeight: '500' }}>Popular tags:</div>
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.35rem' }}>
          {popularInfo?.tags?.map(tag => (
            <button
              key={tag}
              style={{
                padding: '0.3rem 0.5rem',
                background: '#0f4ea8',
                color: '#fff',
                border: 'none',
                borderRadius: '4px',
                cursor: 'pointer',
                fontSize: '0.85rem'
              }}
              onClick={() => runSearch(tag)}
            >
              {tag}
            </button>
          ))}
        </div>
      </div>
    </div>
    <div style={{ border: '2px solid #1976d2', borderRadius: '8px', padding: '1rem', margin: '1rem', background: '#181818ff', maxWidth: '600px', margin: '0 auto' }}>
      <div style={{ borderBottom: '1px solid #ccc', textAlign: 'left', padding: '8px' }}>Results</div>
      <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem', marginTop: '0.75rem' }}>
        {(searchResult?.roms?.length ? searchResult.roms : []).map((rom, index) => {
          const cardKey = rom?.romCid;
          const expiresSoon = (rom.storageExpiryUtc - Date.now()) < 24 * 60 * 60 * 1000;
          return (
            <div
              key={cardKey}
              style={{
                width: '90%',
                border: '1px solid #2c2c2c',
                borderRadius: '8px',
                padding: '0.75rem',
                background: '#0f0f0f',
                boxShadow: '0 1px 4px rgba(0,0,0,0.2)',
                textAlign: 'left'
              }}
            >
              <div style={{display: 'grid',gridTemplateColumns: '120px 1fr',alignItems: 'start',gap: '0.5rem',padding: '0.15rem 0'}}>
                <div style={{ color: '#a8c7ff', fontWeight: 600, textTransform: 'capitalize' }}>Title</div>
                <div style={{ color: '#e0e0e0', wordBreak: 'break-word' }}>{rom.info.title}</div>
              </div>

              <div style={{display: 'grid',gridTemplateColumns: '120px 1fr',alignItems: 'start',gap: '0.5rem',padding: '0.15rem 0'}}>
                <div style={{ color: '#a8c7ff', fontWeight: 600, textTransform: 'capitalize' }}>Author</div>
                <div style={{ color: '#e0e0e0', wordBreak: 'break-word' }}>{rom.info.author}</div>
              </div>

              <div style={{display: 'grid',gridTemplateColumns: '120px 1fr',alignItems: 'start',gap: '0.5rem',padding: '0.15rem 0'}}>
                <div style={{ color: '#a8c7ff', fontWeight: 600, textTransform: 'capitalize' }}>Tags</div>
                <div style={{ color: '#e0e0e0', wordBreak: 'break-word' }}>{rom.info.tags}</div>
              </div>

              <div style={{ color: '#e0e0e0', wordBreak: 'break-word' }}>{rom.info.description}</div>
              <div style={{ color: '#e0e0e0', wordBreak: 'break-word' }}>{rom.entries.length} files</div>
              <div style={{ display: 'flex', justifyContent: 'flex-end', alignItems: 'center', gap: '0.5rem', marginTop: '0.5rem' }}>
                {(expiresSoon && <span style={{ color: '#f3c989', fontSize: '0.8rem' }}>expires soon</span>)}
                <button
                  onClick={() => handleOpenRom(rom.romCid)}
                  style={{
                    display: 'inline-flex',
                    alignItems: 'center',
                    gap: '0.35rem',
                    padding: '0.4rem 0.75rem',
                    background: '#1976d2',
                    color: '#fff',
                    border: 'none',
                    borderRadius: '6px',
                    cursor: 'pointer'
                  }}
                >
                  <span>Open</span>
                  <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <polyline points="9 18 15 12 9 6" />
                  </svg>
                </button>
              </div>
            </div>
          );
        })}
        {(!searchResult?.roms || searchResult.roms.length === 0) && (
          <div style={{ color: '#aaa', textAlign: 'center', padding: '0.5rem' }}>
            No results yet. Try a search or load popular ROMs.
          </div>
        )}
      </div>
    </div>
  </>
}
