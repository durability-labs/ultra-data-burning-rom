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

  // Wrap setUsername to update state
  const setUsername = (name) => {
    setUsernameState(name);
  };

  const clearUsername = () => {
    setUsernameState('');
  }

  return (
    <AppContext.Provider value={{ username, setUsername, clearUsername }}>
      {children}
    </AppContext.Provider>
  );
}

export function useAppContext() {
  return useContext(AppContext);
}
