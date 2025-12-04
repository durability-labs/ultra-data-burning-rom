
import React from 'react';
import './App.css';
import { useAppContext } from './AppContext';
import LoginDialog from './Login';
import Home from './Home';

function App() {
    const { username, setUsername } = useAppContext();
    return (
        <>
            {!username ? (
                <LoginDialog onAccept={setUsername} />
            ) : (
                <Home />
            )}
        </>
    );
}

export default App;
