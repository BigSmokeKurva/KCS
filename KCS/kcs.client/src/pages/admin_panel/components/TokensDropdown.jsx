import styles from './InviteCodeTimeDropdown.module.css';
import _styles from './TokensDropdown.module.css';
import classNames from 'classnames';
import { useState, useRef, useEffect } from 'react';

function TokensDropdown({ items }) {
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
        <div className={classNames(
            styles.dropdown_menu,
            _styles.dropdown_menu
        )} ref={dropdownRef}>
            <button className={classNames(
                styles.toggle_dropdown_button,
                _styles.toggle_dropdown_button,
                isOpen && styles.open,
                isOpen && _styles.open
            )} onClick={toggleDropdown}>
                <svg width="21" height="21" viewBox="0 0 21 21" fill="none" xmlns="http://www.w3.org/2000/svg">
                    <path fillRule="evenodd" clipRule="evenodd" d="M10.5002 7.16671C11.4168 7.16671 12.1668 6.41671 12.1668 5.50004C12.1668 4.58337 11.4168 3.83337 10.5002 3.83337C9.5835 3.83337 8.8335 4.58337 8.8335 5.50004C8.8335 6.41671 9.5835 7.16671 10.5002 7.16671ZM10.5002 8.83337C9.5835 8.83337 8.8335 9.58337 8.8335 10.5C8.8335 11.4167 9.5835 12.1667 10.5002 12.1667C11.4168 12.1667 12.1668 11.4167 12.1668 10.5C12.1668 9.58337 11.4168 8.83337 10.5002 8.83337ZM8.8335 15.5C8.8335 14.5834 9.5835 13.8334 10.5002 13.8334C11.4168 13.8334 12.1668 14.5834 12.1668 15.5C12.1668 16.4167 11.4168 17.1667 10.5002 17.1667C9.5835 17.1667 8.8335 16.4167 8.8335 15.5Z" fill="white" fillOpacity="0.7" />
                </svg>
            </button>
            {
                isRendered &&
                <div ref={dropdownContentRef} className={classNames(
                    styles.dropdown_content,
                    _styles.dropdown_content,
                    isOpen ? styles.open : styles.closed,
                )} onClick={toggleDropdown}>
                    {
                        items.map((item, index) => {
                            return (
                                <button key={index} onClick={item.callback}>{item.title}</button>
                            )
                        })
                    }
                </div>
            }
        </div>
    )
}

export default TokensDropdown;