import styles from './InviteCodeTimeDropdown.module.css';
import _styles from './ManagmentDropdown.module.css';
import classNames from 'classnames';
import { useState, useRef, useEffect } from 'react';
import Checkbox from './Checkbox';

function ManagmentDropdown({ paused, editPaused, admin, editAdmin, deleteUser }) {
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
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="17" viewBox="0 0 16 17" fill="none">
                    <path d="M8 6.5C7.46957 6.5 6.96086 6.71071 6.58579 7.08579C6.21071 7.46086 6 7.96957 6 8.5C6 9.03043 6.21071 9.53914 6.58579 9.91421C6.96086 10.2893 7.46957 10.5 8 10.5C8.53043 10.5 9.03914 10.2893 9.41421 9.91421C9.78929 9.53914 10 9.03043 10 8.5C10 7.96957 9.78929 7.46086 9.41421 7.08579C9.03914 6.71071 8.53043 6.5 8 6.5Z" fill="#EFEFF1" />
                    <path fillRule="evenodd" clipRule="evenodd" d="M7 0.5H9C8.99992 0.89654 9.11713 1.28425 9.33689 1.61432C9.55664 1.9444 9.86912 2.20211 10.235 2.355L10.765 2.575C11.1319 2.72554 11.5351 2.76418 11.9239 2.68606C12.3127 2.60795 12.6697 2.41656 12.95 2.136L14.364 3.55C14.0834 3.83028 13.8921 4.18728 13.8139 4.57608C13.7358 4.96489 13.7745 5.36811 13.925 5.735L14.145 6.265C14.2979 6.63088 14.5556 6.94336 14.8857 7.16311C15.2158 7.38287 15.6035 7.50008 16 7.5V9.5C15.6035 9.49992 15.2158 9.61713 14.8857 9.83689C14.5556 10.0566 14.2979 10.3691 14.145 10.735L13.925 11.265C13.7746 11.632 13.7361 12.0353 13.8144 12.4241C13.8927 12.8129 14.0843 13.1698 14.365 13.45L12.95 14.864C12.6698 14.5836 12.313 14.3924 11.9244 14.3143C11.5358 14.2362 11.1328 14.2747 10.766 14.425L10.235 14.645C9.86912 14.7979 9.55664 15.0556 9.33689 15.3857C9.11713 15.7158 8.99992 16.1035 9 16.5H7C6.99989 16.1036 6.88258 15.7161 6.66284 15.3863C6.44309 15.0564 6.13072 14.7988 5.765 14.646L5.235 14.426C4.86823 14.2752 4.46503 14.2363 4.0762 14.3143C3.68738 14.3922 3.33031 14.5835 3.05 14.864L1.636 13.45C1.91631 13.1698 2.10746 12.8129 2.1854 12.4243C2.26334 12.0356 2.22458 11.6326 2.074 11.266L1.854 10.735C1.70117 10.3693 1.44362 10.0569 1.11374 9.83716C0.783858 9.61742 0.39637 9.50011 0 9.5V7.5C0.809 7.5 1.545 7.013 1.854 6.265L2.074 5.735C2.22479 5.36823 2.26367 4.96503 2.18573 4.5762C2.10778 4.18738 1.91651 3.83031 1.636 3.55L3.05 2.136C3.33039 2.41639 3.68745 2.60759 4.07624 2.68553C4.46504 2.76347 4.86821 2.72466 5.235 2.574L5.765 2.354C6.13072 2.20117 6.44309 1.94362 6.66284 1.61374C6.88258 1.28386 6.99989 0.89637 7 0.5ZM3 8.5L4.464 12.036L8 13.5L11.535 12.036L13 8.5L11.535 4.964L8 3.5L4.464 4.964L3 8.5Z" fill="#EFEFF1" />
                </svg>
            </button>
            {
                isRendered &&
                <div ref={dropdownContentRef} className={classNames(
                    styles.dropdown_content,
                    _styles.dropdown_content,
                    isOpen ? styles.open : styles.closed,
                )}>
                    <button className={_styles.button_with_checkbox} onClick={editPaused}>
                        Пауза
                        <Checkbox checked={paused} />
                    </button>
                    <button className={_styles.button_with_checkbox} onClick={editAdmin}>
                        Права админа
                        <Checkbox checked={admin} />
                    </button>
                    <div className={_styles.separator}></div>
                    <button className={_styles.delete_button} onClick={deleteUser}>
                        Удалить
                    </button>
                </div>
            }
        </div>
    )
}

export default ManagmentDropdown;