import React, {useEffect} from 'react';
import { useAppContext } from './AppContext';

export default function Catalogue() {
  const { popularInfo, setPopularInfo, searchResult, setSearchResult } = useAppContext();

  useEffect(() => {
    if (!popularInfo) {
      fetch('/catalogue')
        .then(res => res.json())
        .then(data => {
          setPopularInfo(data);
        })
        .catch(() => {
        });
    }
  }, []);

  return <>
    <div style={{ border: '2px solid #1976d2', borderRadius: '8px', padding: '1rem', margin: '1rem', background: '#181818ff', maxWidth: '600px', margin: '0 auto' }}>
      <div style={{ borderBottom: '1px solid #ccc', textAlign: 'left', padding: '8px' }}>Search</div>
      <input
        type="text"
        placeholder="Search the catalogue..."
      />
      <div>
        <button style={{
                padding: '0.5rem 0.75rem',
                background: '#1976d2',
                color: '#fff',
                border: 'none',
                borderRadius: '6px',
                cursor: 'pointer'
            }}>Popular ROMs</button>
        <div>Popular tags:</div>
        {/* Tag buttons here */}
      </div>
    </div>
  </>
}
