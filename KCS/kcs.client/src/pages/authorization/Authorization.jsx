import React, { useCallback } from 'react';
import SwitchComponent from './components/Switch';
import styles from './style.module.css';
import SignUp from './components/SignUp';
import SignIn from './components/SignIn';
import { useLocation, useNavigate } from 'react-router-dom';
import logoSvg from "../../assets/logo.svg";


function Authorization() {
    const location = useLocation();
    const page = location.pathname.replace("/", "");
    const navigate = useNavigate();

    const checkAuth = useCallback(async () => {
        var response = await fetch('/api/auth/checkauth', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        });
        if (response.redirected) {
            window.location.href = response.url;
        }
        var result = await response.json();
        if (result.auth) {
            navigate('/app');
        }
    }, []);

    React.useEffect(() =>{
        checkAuth();
    }, []);

    return (
        <div className={styles.main_container} >
            <div className={styles.logo_container}>
                <img src={logoSvg} />
                <div className={styles.logo_title}>
                    <span>RBT</span>
                    <span>Chat</span>
                </div>
            </div>
            <SwitchComponent _selectedMode={page} />
            {page === "signup" ? <SignUp /> : <SignIn />}
        </div>
    );
}

export default Authorization;
