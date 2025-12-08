import React, { useState, useMemo, useEffect, useCallback } from 'react';
import UploadButton from './UploadButton';
import { useAppContext } from './AppContext';
import BurnButton from './BurnButton';
import UploadOpen from './UploadOpen';
import UploadBusy from './UploadBusy';

const defaultBucket = {
    entries: [],
    volumeSize: 1,
    state: 0,
    expiryUtc: 0
  };

export default function Upload() {
  const { username } = useAppContext();
  const [bucket, setBucket] = useState(defaultBucket);

  const updateBucket = useCallback((user) => {
    if (!user) return;
    fetch(`/bucket/${user}`)
      .then(res => res.json())
      .then(data => setBucket(data))
      .catch(() => setBucket(defaultBucket));
  }, []);

  const bytesUsed = useMemo(() => {
    if (!bucket) return 0;
    return bucket.entries.reduce((total, file) => total + file.byteSize, 0);
  }, [bucket]);

  useEffect(() => {
    if (!username) return;
    updateBucket(username);    
  }, [username]);

  // Poll the bucket every 3 seconds while a username is present
  useEffect(() => {
    if (!username) return;
    const id = setInterval(() => updateBucket(username), 3000);
    return () => clearInterval(id);
  }, [username, updateBucket]);

  if (bucket.state > 0) return UploadBusy(username, bucket);
  return UploadOpen(username, bucket, bytesUsed);
}
