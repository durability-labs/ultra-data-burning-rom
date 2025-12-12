import React, { useState, useMemo, useEffect } from 'react';
import UploadButton from './UploadButton';
import { useAppContext } from './AppContext';
import BurnButton from './BurnButton';

const bucketStates = {
  0: 'Unknown',
  1: 'Open',
  2: 'Starting',
  3: 'Compressing',
  4: 'Uploading',
  5: 'Purchasing',
  6: 'Done'
}

export default function UploadBusy(username, setActiveTab, setRomCid, bucket) {
  async function handleOpen(bucket) {
    if (!username) return;
    try {
      await fetch(`/bucket/${username}/clear`, {
        method: 'POST'
      });
    } catch (err) {
      console.error('Error sending burn request:', err);
    } 

    setRomCid(bucket.romCid);
    setActiveTab(2);
  }

  return (
    <>
      <div style={{ color: 'rgba(238, 238, 238, 1)', border: '2px solid #1976d2', borderRadius: '8px', padding: '1rem', background: '#181818ff', maxWidth: '600px', margin: '0 auto' }}>
        <div>
          Burning ROM...
        </div>
        <div>
         {bucketStates[bucket.state]}
        </div>
        <div>
          This can take up to 20 minutes...
        </div>
      </div>
      {(bucket.romCid &&
        <div style={{ border: '2px solid #6bb1f7ff', borderRadius: '8px', padding: '1rem', background: '#181818ff', margin: '1rem' }}>
            <strong>Finished! ROM CID:</strong> {bucket.romCid}
            <div>
              <button
                style={{
                  marginTop: '0.5rem',
                  padding: '0.4rem 0.8rem',
                  background: '#1976d2',
                  color: '#fff',
                  border: 'none',
                  borderRadius: '6px',
                  cursor: 'pointer'
                }}
                onClick={() => handleOpen(bucket)}>
                View your new ROM
              </button>
            </div>
        </div>
      )}
    </>
  );
}
