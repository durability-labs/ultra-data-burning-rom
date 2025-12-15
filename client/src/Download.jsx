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

  const [isDownloadModalOpen, setIsDownloadModalOpen] = useState(false);
  const [downloadProgress, setDownloadProgress] = useState(0);
  const [downloadStatus, setDownloadStatus] = useState('idle'); // 'idle' | 'downloading' | 'success' | 'error'
  const [downloadFileName, setDownloadFileName] = useState('');
  const [downloadError, setDownloadError] = useState('');

  const resetDownloadState = () => {
    setIsDownloadModalOpen(false);
    setDownloadProgress(0);
    setDownloadStatus('idle');
    setDownloadFileName('');
    setDownloadError('');
  };

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

    setIsDownloadModalOpen(true);
    setDownloadStatus('downloading');
    setDownloadProgress(0);
    setDownloadFileName(file.filename || 'file');
    setDownloadError('');

    try {
      const xhr = new XMLHttpRequest();
      xhr.open('POST', `/rom/${username}/${romCid}/file`);
      xhr.responseType = 'blob';
      xhr.setRequestHeader('Content-Type', 'application/json');

      xhr.onprogress = (e) => {
        if (e.lengthComputable) {
          const percent = Math.round((e.loaded / e.total) * 100);
          setDownloadProgress(percent);
        }
      };

      xhr.onload = () => {
        if (xhr.status >= 200 && xhr.status < 300) {
          const blob = xhr.response;
          const url = URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = file.filename || 'file';
          document.body.appendChild(a);
          a.click();
          a.remove();
          URL.revokeObjectURL(url);

          setDownloadProgress(100);
          setDownloadStatus('success');
        } else {
          console.error('Download failed', xhr.status, xhr.statusText);
          setDownloadStatus('error');
          setDownloadError('Download failed.');
        }
      };

      xhr.onerror = () => {
        console.error('Error downloading file');
        setDownloadStatus('error');
        setDownloadError('Error downloading file.');
      };

      xhr.send(
        JSON.stringify({
          filename: file.filename
        })
      );
    } catch (err) {
      console.error('Error downloading file', err);
      setDownloadStatus('error');
      setDownloadError('Error downloading file.');
    }
  }, [username, romCid]);

  const handleDownloadRom = useCallback(async () => {
    if (!username || !romCid) return;

    setIsDownloadModalOpen(true);
    setDownloadStatus('downloading');
    setDownloadProgress(0);
    setDownloadFileName(romCid || 'rom');
    setDownloadError('');

    try {
      const xhr = new XMLHttpRequest();
      xhr.open('POST', `/rom/${username}/${romCid}/all`);
      xhr.responseType = 'blob';

      xhr.onprogress = (e) => {
        if (e.lengthComputable) {
          const percent = Math.round((e.loaded / e.total) * 100);
          setDownloadProgress(percent);
        }
      };

      xhr.onload = () => {
        if (xhr.status >= 200 && xhr.status < 300) {
          const blob = xhr.response;
          const url = URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = romCid;
          document.body.appendChild(a);
          a.click();
          a.remove();
          URL.revokeObjectURL(url);

          setDownloadProgress(100);
          setDownloadStatus('success');
        } else {
          console.error('Download failed', xhr.status, xhr.statusText);
          setDownloadStatus('error');
          setDownloadError('Download failed.');
        }
      };

      xhr.onerror = () => {
        console.error('Error downloading file');
        setDownloadStatus('error');
        setDownloadError('Error downloading file.');
      };

      xhr.send();
    } catch (err) {
      console.error('Error downloading file', err);
      setDownloadStatus('error');
      setDownloadError('Error downloading file.');
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
    <div style={{ color: 'rgba(238, 238, 238, 1)', border: '2px solid #1976d2', borderRadius: '8px', padding: '1rem', margin: '1rem', background: '#181818ff', maxWidth: '600px', margin: '0 auto' }}>
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
    <div style={{ color: 'rgba(238, 238, 238, 1)', border: '2px solid #1976d2', borderRadius: '8px', padding: '1rem', margin: '1rem', background: '#181818ff', maxWidth: '600px', margin: '0 auto' }}>
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

      {isDownloadModalOpen && (
        <div
          style={{
            position: 'fixed',
            inset: 0,
            backgroundColor: 'rgba(0, 0, 0, 0.4)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 1000
          }}
        >
          <div
            role="dialog"
            aria-modal="true"
            style={{
              minWidth: '280px',
              maxWidth: '400px',
              background: '#eee',
              color: '#000',
              borderRadius: '8px',
              padding: '1rem 1.25rem',
              boxShadow: '0 8px 24px rgba(0,0,0,0.18)'
            }}
          >
            {downloadStatus === 'downloading' && (
              <div>
                <div style={{ marginBottom: '0.5rem', fontWeight: 500 }}>
                  Downloading {downloadFileName || 'file'}...
                </div>
                <div
                  style={{
                    width: '100%',
                    height: '10px',
                    borderRadius: '999px',
                    background: '#e0e0e0',
                    overflow: 'hidden',
                    margin: '0.5rem 0 0.25rem'
                  }}
                >
                  <div
                    style={{
                      width: `${downloadProgress}%`,
                      height: '100%',
                      background: '#1976d2',
                      transition: 'width 0.2s ease'
                    }}
                  />
                </div>
                <div style={{ fontSize: '0.85rem', color: '#555' }}>
                  {downloadProgress}%
                </div>
              </div>
            )}

            {downloadStatus === 'success' && (
              <div>
                <div style={{ marginBottom: '0.5rem', fontWeight: 500 }}>
                  Download complete
                </div>
                <div style={{ fontSize: '0.9rem', marginBottom: '1rem' }}>
                  {downloadFileName} has been downloaded successfully.
                </div>
                <button
                  onClick={resetDownloadState}
                  style={{
                    padding: '0.35rem 1.25rem',
                    fontSize: '0.9rem',
                    background: '#1976d2',
                    color: '#fff',
                    border: 'none',
                    borderRadius: '4px',
                    cursor: 'pointer'
                  }}
                >
                  Close
                </button>
              </div>
            )}

            {downloadStatus === 'error' && (
              <div>
                <div style={{ marginBottom: '0.5rem', fontWeight: 500, color: '#d32f2f' }}>
                  Download failed
                </div>
                <div style={{ fontSize: '0.9rem', marginBottom: '1rem' }}>
                  {downloadError || 'Something went wrong during the download.'}
                </div>
                <button
                  onClick={resetDownloadState}
                  style={{
                    padding: '0.35rem 1.25rem',
                    fontSize: '0.9rem',
                    background: '#d32f2f',
                    color: '#fff',
                    border: 'none',
                    borderRadius: '4px',
                    cursor: 'pointer'
                  }}
                >
                  Close
                </button>
              </div>
            )}
          </div>
        </div>
      )}
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
        (rom.mountState === 4) &&
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
          <div style={{ color: 'rgba(238, 238, 238, 1)', maxWidth: '600px', margin: '1.5rem auto 1.5rem auto', textAlign: 'center' }}>
            Spinning it up... Stand by... (This can take a few minutes)
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
