import { useState, useRef, useEffect } from 'react';
import styles from '../style.module.css';
import _styles from "./LogsSwitch.module.css";
import classNames from 'classnames';

function LogsSwitch({ selectedMode, setSelectedMode }) {
    const underlineRef = useRef(null);
    const historyChatRef = useRef(null);
    const historyActionRef = useRef(null);

    function clickHandler(event) {
        const mode = event.target.id.replace('-btn', '');
        if (mode === selectedMode) return;
        switchToSelected(
            event.target,
        );
        setSelectedMode(mode);
    }

    function switchToSelected(selectedElement) {
        const buttonRect = selectedElement.getBoundingClientRect();
        const containerRect = selectedElement.parentElement.getBoundingClientRect();
        const leftOffset = buttonRect.left - containerRect.left;

        underlineRef.current.style.width = buttonRect.width + 24 + 'px';
        underlineRef.current.style.left = leftOffset - 12 + 'px';
    }

    useEffect(() => {
        setTimeout(() => switchToSelected(selectedMode === "chat" ? historyChatRef.current : historyActionRef.current), 150);
    }, []);

    return (
        <div className={styles.switch_container}>
            <button className={classNames(
                styles.switch_button,
                _styles.switch_button,
                selectedMode === "chat" && styles.selected
            )} id="chat-btn" ref={historyChatRef} onClick={clickHandler}>
                История чата
            </button>
            <button className={classNames(
                styles.switch_button,
                _styles.switch_button,
                selectedMode === "action" && styles.selected
            )} id="action-btn" ref={historyActionRef} onClick={clickHandler}>
                История действий
            </button>
            <div className={styles.switch_underlines_container}>
                <div className={styles.switch_background_underline}></div>
                <div className={styles.switch_selected_underline} ref={underlineRef}></div>
            </div>
        </div>
    )
}

export default LogsSwitch;