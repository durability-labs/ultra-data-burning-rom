import React, { useState, useMemo, useEffect } from 'react';
import UploadButton from './UploadButton';
import { useAppContext } from './AppContext';
import BurnButton from './BurnButton';

export default function UploadBusy(username, bucket) {
  return (
    <>
      <div style={{ border: '2px solid #1976d2', borderRadius: '8px', padding: '1rem', background: '#181818ff', maxWidth: '600px', margin: '0 auto' }}>
        Busy... {bucket.state}
      </div>
    </>
  );
}
