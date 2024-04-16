import { useEffect, useRef } from 'react';
import styles from "../style.module.css";
import streamOfflineSvg from "../../../assets/stream_offline.svg";

function Stream({ streamerUsername, isOnline }) {
    const iframeRef = useRef(null);

    useEffect(() => {
        const setIframeHeight = () => {
            const width = iframeRef.current.offsetWidth;
            const height = width * 9 / 16;
            iframeRef.current.style.height = `${height}px`;
        };
        setIframeHeight();
        window.addEventListener('resize', setIframeHeight);
        return () => {
            window.removeEventListener('resize', setIframeHeight);
        };
    }, [streamerUsername, isOnline]);

    return (
        (isOnline) ? (
            <iframe
                ref={iframeRef}
                src={`https://player.twitch.tv/?channel=${streamerUsername}&parent=${window.location.hostname}`}
                width="100%"
                frameBorder="0"
                scrolling="no"
                allowFullScreen={false}
            />
        ) : (
            <div className={styles.stream_offline} ref={iframeRef}>
                <img src={streamOfflineSvg} alt="offline" />
                Стрим оффлайн
            </div>
        )
    );
}

export default Stream;
