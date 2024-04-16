import { useState, useRef, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import styles from '../style.module.css';
import classNames from 'classnames';

function SwitchComponent({ _selectedMode }) {
    const [selectedMode, setSelectedMode] = useState(_selectedMode);
    const underlineRef = useRef(null);
    const adminPanelRef = useRef(null);
    const inviteCodesRef = useRef(null);
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
        setTimeout(() => switchToSelected(selectedMode === "admin-panel" ? adminPanelRef.current : inviteCodesRef.current), 150);
    }, []);

    return (
        <div className={styles.switch_container}>
            <button className={classNames(
                styles.switch_button,
                selectedMode === "admin-panel" && styles.selected
            )} id="admin-panel-btn" ref={adminPanelRef} onClick={clickHandler}>
                Сотрудники
            </button>
            <button className={classNames(
                styles.switch_button,
                selectedMode === "invite-codes" && styles.selected
            )} id="invite-codes-btn" ref={inviteCodesRef} onClick={clickHandler}>
                Инвайт-коды
            </button>
            <div className={styles.switch_underlines_container}>
                <div className={styles.switch_background_underline}></div>
                <div className={styles.switch_selected_underline} ref={underlineRef}></div>
            </div>
        </div>
    )
}

export default SwitchComponent;