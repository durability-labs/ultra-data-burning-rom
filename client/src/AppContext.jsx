import React, { createContext, useContext, useState, useEffect } from 'react';

// Create a context for the app's global state
const AppContext = createContext();

export function AppProvider({ children }) {
  const [username, setUsernameState] = useState(() => {
    return localStorage.getItem('username') || '';
  });

  // Update localStorage whenever username changes
  useEffect(() => {
    if (username) {
      localStorage.setItem('username', username);
    } else {
      localStorage.removeItem('username');
    }
  }, [username]);

  // New global state: active tab index and rom CID (initialize from localStorage)
  const [activeTab, setActiveTab] = useState(() => {
    const v = localStorage.getItem('activeTab');
    return v !== null ? parseInt(v, 10) || 0 : 0;
  });
  const [romCid, setRomCid] = useState(() => {
    return localStorage.getItem('romCid') || '';
  });

  // Persist activeTab to localStorage
  useEffect(() => {
    try {
      localStorage.setItem('activeTab', String(activeTab));
    } catch (e) {
      // ignore
    }
  }, [activeTab]);

  // Persist romCid to localStorage (remove if empty)
  useEffect(() => {
    try {
      if (romCid) localStorage.setItem('romCid', romCid);
      else localStorage.removeItem('romCid');
    } catch (e) {
      // ignore
    }
  }, [romCid]);

  // Wrap setUsername to update state
  const setUsername = (name) => {
    setUsernameState(name);
  };

  const clearUsername = () => {
    setUsernameState('');
  }

  const [popularInfo, setPopularInfo] = useState(() => {
    return JSON.parse(localStorage.getItem('popularinfo')) || {
      roms: [],
      tags: []
    };
  });
  useEffect(() => {
    if (popularInfo) {
      localStorage.setItem('popularinfo', JSON.stringify(popularInfo));
    }
  }, [popularInfo]);

  const [searchResult, setSearchResult] = useState(() => {
    return JSON.parse(localStorage.getItem('searchresult')) || {
      roms: []
    };
  });
  useEffect(() => {
    if (searchResult) {
      localStorage.setItem('searchresult', JSON.stringify(searchResult));
    }
  }, [searchResult]);

  return (
    <AppContext.Provider value={{ username, setUsername, clearUsername, activeTab, setActiveTab, romCid, setRomCid, popularInfo, setPopularInfo, searchResult, setSearchResult }}>
      {children}
    </AppContext.Provider>
  );
}

export function useAppContext() {
  return useContext(AppContext);
}
