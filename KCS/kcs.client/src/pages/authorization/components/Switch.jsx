import { useState, useRef, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import styles from '../style.module.css';
import classNames from 'classnames';

export default function SwitchComponent({ _selectedMode }) {
    const [selectedMode, setSelectedMode] = useState(_selectedMode);
    const underlineRef = useRef(null);
    const signupRef = useRef(null);
    const signinRef = useRef(null);
    const navigate = useNavigate();

    function clickHandler(event) {
        const mode = event.target.id.replace('-btn', '');
        if (mode === selectedMode) return;
        switchToSelected(
            event.target,
        );
        setSelectedMode(mode);
        navigate(`/${mode}`);
    }

    function switchToSelected(selectedElement) {
        const buttonRect = selectedElement.getBoundingClientRect();
        const containerRect = selectedElement.parentElement.getBoundingClientRect();
        const leftOffset = buttonRect.left - containerRect.left;

        underlineRef.current.style.width = buttonRect.width + 24 + 'px';
        underlineRef.current.style.left = leftOffset - 12 + 'px';
    }

    useEffect(() => {
        setTimeout(() => switchToSelected(selectedMode === "signup" ? signupRef.current : signinRef.current), 150);
    }, []);

    return (
        <div className={styles.switch_container}>
            <button className={classNames(styles.switch_button, selectedMode === "signup" && styles.selected)} id="signup-btn" ref={signupRef} onClick={clickHandler}>
                Регистрация
            </button>
            <button className={classNames(styles.switch_button, selectedMode === "signin" && styles.selected)} id="signin-btn" ref={signinRef} onClick={clickHandler}>
                Вход
            </button>
            <div className={styles.switch_underlines_container}>
                <div className={styles.switch_background_underline}></div>
                <div className={styles.switch_selected_underline} ref={underlineRef}></div>
            </div>
        </div>
    )
}