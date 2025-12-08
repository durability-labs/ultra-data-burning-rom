import React, { useState, useCallback, useEffect } from 'react';
import { useAppContext } from './AppContext';

const defaultRom = {
    romCid: "",
    mounted: false,
    info: {
      title: "",
      author: "",
      tags: "",
      description: ""
    },
    entries: [],
    expiryUtc: 0
  };

export default function Download() {
  const { username,  romCid } = useAppContext();
  const [rom, setRom] = useState(defaultRom);

  const updateRom = useCallback((user, cid) => {
    if (!user) return;
    if (!cid) return;
    fetch(`/rom/${user}/${cid}`)
      .then(res => res.json())
      .then(data => setRom(data))
      .catch(() => setRom(defaultRom));
  }, []);

  useEffect(() => {
    if (!username) return;
    if (!romCid) return;
    updateRom(username, romCid);    
  }, [username, romCid]);

  // Poll the rom every 3 seconds
  useEffect(() => {
    if (!username) return;
    if (!romCid) return;
    const id = setInterval(() => updateRom(username, romCid), 3000);
    return () => clearInterval(id);
  }, [username, romCid, updateRom]);

  if (rom.romCid.length === 0) {
    return <div>No ROM loaded.</div>;
  }

  return <>
    <div style={{ border: '2px solid #1976d2', borderRadius: '8px', padding: '1rem', background: '#181818ff', maxWidth: '600px', margin: '0 auto' }}>
      <label style={{ display: 'block', marginBottom: '0.5rem' }}>ROM CID</label>
      <div>{romCid}</div>

      <table style={{ width: '100%', borderCollapse: 'collapse', tableLayout: 'fixed' }}>
        <tbody>
          <tr>
            <td style={{ padding: '0.5rem', borderBottom: '1px solid #444', width: '25%', textAlign: 'left' }}>Title</td>
            <td style={{ padding: '0.5rem', borderBottom: '1px solid #444', width: '75%', textAlign: 'left' }}>{rom.info.title}</td>
          </tr>
          <tr>
            <td style={{ padding: '0.5rem', borderBottom: '1px solid #444', width: '25%', textAlign: 'left' }}>Author</td>
            <td style={{ padding: '0.5rem', borderBottom: '1px solid #444', width: '75%', textAlign: 'left' }}>{rom.info.author}</td>
          </tr>
          <tr>
            <td style={{ padding: '0.5rem', borderBottom: '1px solid #444', width: '25%', textAlign: 'left' }}>Tags</td>
            <td style={{ padding: '0.5rem', borderBottom: '1px solid #444', width: '75%', textAlign: 'left' }}>{rom.info.tags}</td>
          </tr>
          <tr>
            <td style={{ padding: '0.5rem', width: '25%', textAlign: 'left' }}>Description</td>
            <td style={{ padding: '0.5rem', width: '75%', textAlign: 'left' }}>{rom.info.description}</td>
          </tr>
        </tbody>
      </table>

      <div style={{ marginTop: '1rem' }}>
        Placeholder for download actions
      </div>
    </div>
    </>;
}
