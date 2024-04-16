import styles from "./style.module.css";
import pauseSvg from "../../assets/pause.svg"
import { useEffect, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import Cookies from 'js-cookie';

function Pause() {
    const navigate = useNavigate();
    const checkIsPause = useCallback(async () => {
        const auth_token = Cookies.get('auth_token');
        var response = await fetch('/api/app/checkispause', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': auth_token
            }
        });
        var result = await response.json();
        if (!result.isPause) {
            navigate('/app');
        }
    }, [navigate]);

    useEffect(() => {
        checkIsPause();
        const interval = setInterval(checkIsPause, 10000);
        return () => {
            clearInterval(interval);
        }
    }, []);

    return (
        <div className={styles.main_container}>
            <img src={pauseSvg} alt="pause" />
            <span>Аккаунт приостановлен</span>
        </div>
    )
}

export default Pause;