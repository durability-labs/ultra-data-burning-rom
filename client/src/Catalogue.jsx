import React, {useEffect, useState} from 'react';
import { useAppContext } from './AppContext';

export default function Catalogue() {
  const { popularInfo, setPopularInfo, searchResult, setSearchResult } = useAppContext();
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
    setSearchResult(popularInfo);
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
  </>
}
