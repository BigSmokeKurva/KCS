import { useState, useRef, useEffect } from "react";
import styles from "./InviteCodeTimeDropdown.module.css";
import _styles from "./LogsTimeDropdown.module.css";
import classNames from "classnames";

function LogsTimeDropdown({ selectedTime, setSelectedTime, items, disabled }) {
    const [isOpen, setIsOpen] = useState(false);
    const [isRendered, setIsRendered] = useState(false);
    const dropdownRef = useRef(null);
    const dropdownContentRef = useRef(null);

    function handleClickOutside(event) {
        if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
            toggleDropdown(event, false);
        }
    }

    function toggleDropdown(event, value) {
        const newValue = value === undefined ? !isOpen : value
        setIsOpen(newValue);
        !newValue ? dropdownContentRef.current?.addEventListener('transitionend', () => {
            setIsRendered(newValue);
        }) : setIsRendered(newValue);

    }

    useEffect(() => {
        window.addEventListener('click', handleClickOutside);
        return () => {
            window.removeEventListener('click', handleClickOutside);
        };
    }, []);

    return (
        <div className={styles.dropdown_menu} ref={dropdownRef} disabled={disabled}>
            <button className={classNames(
                styles.toggle_dropdown_button,
                _styles.toggle_dropdown_button,
                isOpen && styles.open
            )} onClick={toggleDropdown}>
                {selectedTime}
                <div>
                    <svg xmlns="http://www.w3.org/2000/svg" width="8" height="14" viewBox="0 0 8 14" fill="none">
                        <path d="M7 8.5L4 11.5L1 8.5L0 9.5L4 13.5L8 9.5L7 8.5Z" fill={disabled ? "#3F4246" : "#B9B9BA"} />
                        <path d="M1 5.5L4 2.5L7 5.5L8 4.5L4 0.5L0 4.5L1 5.5Z" fill={disabled ? "#3F4246" : "#B9B9BA"} />
                    </svg>
                </div>
            </button>
            {
                isRendered &&
                <div ref={dropdownContentRef} className={classNames(
                    styles.dropdown_content,
                    _styles.dropdown_content,
                    isOpen ? styles.open : styles.closed
                )} onClick={toggleDropdown}>
                    {
                        items.map((item) => {
                            return (
                                <button className={
                                    classNames(
                                        selectedTime === item && styles.selected
                                    )
                                } key={item} onClick={() => setSelectedTime(item)}>{item}</button>
                            )
                        })
                    }
                </div>
            }
        </div>
    )
}

export default LogsTimeDropdown;