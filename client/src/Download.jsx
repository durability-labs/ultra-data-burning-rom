import React from 'react';
import { useAppContext } from './AppContext';

export default function Download() {
  const { romCid } = useAppContext();
  return <>
    <div>Download content goes here.</div>
    <div>{romCid}</div>
    </>;
}
