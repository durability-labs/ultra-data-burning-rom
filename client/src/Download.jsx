import React, { useState, useCallback, useEffect } from 'react';
import { useAppContext } from './AppContext';
import DurabilityOptions from './DurabilityOptions';
import formatBytes from './format';

// Mount state:
// 0: Unknown,
// 1: Bucket,
// 2: Downloading,
// 3: OpenInUse,
// 4: ClosedNotUsed

const defaultRom = {
    romCid: "",
    mountState: 0,
    info: {
      title: "",
      author: "",
      tags: "",
      description: ""
    },
    entries: [],
    mountExpiryUtc: 0,
    storageExpiryUtc: 0
  };

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

export default function Download() {
  const { username,  romCid } = useAppContext();
  const [rom, setRom] = useState(defaultRom);
  const [durabilityInfo, setDurabilityInfo] = useState(null);
  const [selectedDurabilityId, setSelectedDurabilityId] = useState(null);
  const [showExtendOptions, setShowExtendOptions] = useState(false);

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

  const handleDownload = useCallback(async (file, idx) => {
    if (!username || !romCid) return;
    try {
      const entryId = file.id;
      const res = await fetch(`/rom/${username}/${romCid}/file/${entryId}`);
      if (!res.ok) {
        console.error('Download failed', res.status, res.statusText);
        return;
      }
      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = file.filename || 'file';
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(url);
    } catch (err) {
      console.error('Error downloading file', err);
    }
  }, [username, romCid]);

  const handleDownloadRom = useCallback(async () => {
        if (!username || !romCid) return;
    try {
      const res = await fetch(`/rom/${username}/${romCid}/all`);
      if (!res.ok) {
        console.error('Download failed', res.status, res.statusText);
        return;
      }
      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = romCid;
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(url);
    } catch (err) {
      console.error('Error downloading file', err);
    }
  }, [username, romCid]);

  const handleUnmount = useCallback(async () => {
    if (!username || !romCid) return;
    try {
      const res = await fetch(`/rom/${encodeURIComponent(username)}/${encodeURIComponent(romCid)}/unmount`, { method: 'POST' });
      if (!res.ok) console.error('Unmount failed', res.status, res.statusText);
    } catch (err) { console.error('Error unmounting', err); }
    updateRom(username, romCid);
  }, [username, romCid, updateRom]);

  const handleMount = useCallback(async () => {
    if (!username || !romCid) return;
    try {
      const res = await fetch(`/rom/${encodeURIComponent(username)}/${encodeURIComponent(romCid)}/mount`, { method: 'POST' });
      if (!res.ok) console.error('Mount failed', res.status, res.statusText);
    } catch (err) { console.error('Error mounting', err); }
    updateRom(username, romCid);
  }, [username, romCid, updateRom]);

  const handleExtendStorage = useCallback(async () => {
    if (!username || !romCid) return;
    setShowExtendOptions(true);
    try {
      const res = await fetch('/durability');
      if (!res.ok) throw new Error('Failed to load durability options');
      const data = await res.json();
      setDurabilityInfo(data || defaultDurabilityInfo);
      setSelectedDurabilityId(data?.options?.[0]?.id ?? null);
    } catch (err) {
      console.error('Error loading durability options', err);
      setDurabilityInfo(defaultDurabilityInfo);
    }
  }, [username, romCid]);

  const handleSelectDurability = useCallback(async (optionId) => {
    if (!username || !romCid) return;
    setSelectedDurabilityId(optionId);
    try {
      const res = await fetch(`/rom/${encodeURIComponent(username)}/${encodeURIComponent(romCid)}/extend/${encodeURIComponent(optionId)}`, { method: 'POST' });
      if (!res.ok) {
        console.error('Extend storage failed', res.status, res.statusText);
        return;
      }
      updateRom(username, romCid);
    } catch (err) {
      console.error('Error extending storage', err);
    } finally {
      setShowExtendOptions(false);
    }
  }, [username, romCid, updateRom]);

  if (rom.romCid.length === 0) {
    return <div>No ROM loaded.</div>;
  }

  return <>
    <div style={{ border: '2px solid #1976d2', borderRadius: '8px', padding: '1rem', margin: '1rem', background: '#181818ff', maxWidth: '600px', margin: '0 auto' }}>
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
    </div>
    <div style={{ border: '2px solid #1976d2', borderRadius: '8px', padding: '1rem', margin: '1rem', background: '#181818ff', maxWidth: '600px', margin: '0 auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr>
              <th style={{ borderBottom: '1px solid #ccc', textAlign: 'left', padding: '8px' }}>Filename</th>
              <th style={{ borderBottom: '1px solid #ccc', textAlign: 'left', padding: '8px' }}>Size</th>
            </tr>
          </thead>
          <tbody>
            {rom.entries.map((file, idx) => (
              <tr key={idx}>
                <td style={{ padding: '8px', borderBottom: '1px solid #eee' }}>{file.filename}</td>
                <td style={{ padding: '8px', borderBottom: '1px solid #eee' }}>
                  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                    <span>{formatBytes(file.byteSize)}</span>
                    {rom.mountState === 3 && (
                      <button
                        onClick={() => handleDownload(file, idx)}
                        title="Download"
                        style={{
                          marginLeft: '8px',
                          background: 'transparent',
                          border: 'none',
                          cursor: 'pointer',
                          padding: '4px'
                        }}
                      >
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                          <path d="M12 3v10" stroke="#fff" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                          <path d="M5 12l7 7 7-7" stroke="#fff" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                          <path d="M5 21h14" stroke="#fff" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                        </svg>
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {(rom.mountState === 3) && 
      <>
        <div style={{ maxWidth: '600px', margin: '1.5rem auto 1.5rem auto', textAlign: 'center' }}>
          <button
            onClick={handleDownloadRom}
            style={{
                padding: '0.5rem 0.75rem',
                background: '#1976d2',
                color: '#fff',
                border: 'none',
                borderRadius: '6px',
                cursor: 'pointer'
            }}
          >
            Download entire ROM
          </button>
        </div>
        <div style={{ maxWidth: '600px', margin: '1.5rem auto 1.5rem auto', textAlign: 'center' }}>
          <button
            onClick={handleUnmount}
            style={{
                padding: '0.5rem 0.75rem',
                background: '#1976d2',
                color: '#fff',
                border: 'none',
                borderRadius: '6px',
                cursor: 'pointer'
            }}
          >
            Unmount
          </button>
        </div>
        <div style={{ marginBottom: '0.5rem', color: '#fff', fontSize: '0.75rem' }}>
          ROM will automatically unmount: {new Date(rom.mountExpiryUtc).toLocaleString()}
        </div>
      </>
      }
      {
        (rom.mountState === 1) &&
        <>
          <div style={{ maxWidth: '600px', margin: '1.5rem auto 1.5rem auto', textAlign: 'center' }}>
            <button
              onClick={handleMount}
              style={{
                padding: '0.5rem 0.75rem',
                background: '#1976d2',
                color: '#fff',
                border: 'none',
                borderRadius: '6px',
                cursor: 'pointer'
              }}
            >
              Mount
            </button>
          </div>
        </>
      }
      {
        (rom.mountState === 2) &&
        <>
          <div style={{ maxWidth: '600px', margin: '1.5rem auto 1.5rem auto', textAlign: 'center' }}>
            Spinning it up... Stand by...
          </div>
        </>
      }
      <div style={{ marginBottom: '0.5rem', color: '#fff', fontSize: '0.75rem' }}>
        ROM will expire: {new Date(rom.storageExpiryUtc).toLocaleString()}
      </div>
      {(((rom.storageExpiryUtc - Date.now()) < 24 * 60 * 60 * 1000) && 
        <div>          
            <button
            onClick={handleExtendStorage}
            style={{
                padding: '0.5rem 0.75rem',
                background: '#1976d2',
                color: '#fff',
                border: 'none',
                borderRadius: '6px',
                cursor: 'pointer'
            }}
          >
            Renew ROM Storage
          </button>
          {showExtendOptions && (
            <>
              {DurabilityOptions(durabilityInfo, selectedDurabilityId, handleSelectDurability)}
              <div style={{ marginTop: '1rem' }}>
                <button
                  onClick={() => setShowExtendOptions(false)}
                  style={{
                    padding: '0.5rem 0.75rem',
                    background: '#2a2a2a',
                    color: '#fff',
                    border: '1px solid #444',
                    borderRadius: '6px',
                    cursor: 'pointer'
                  }}
                >
                  Cancel
                </button>
              </div>
            </>
          )}
        </div>)}
  </>;
}
