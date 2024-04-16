import { useState, useRef, useEffect, useCallback, useContext } from "react";
import styles from "./BindsTemplateDropdown.module.css";
import _styles from "../../style.module.css";
import classNames from "classnames";
import Cookies from "js-cookie";
import { NotificationsContext } from "../../../../contexts/notification/NotificationsContext";

function BindsTemplateDropdown({ binds, selectedBind, setSelectedBind, getBinds }) {
    /* Переименовать */
    const [isOpen, setIsOpen] = useState(false);
    const [isRendered, setIsRendered] = useState(false);
    const dropdownRef = useRef(null);
    const dropdownContentRef = useRef(null);
    const { showNotification } = useContext(NotificationsContext);

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

    const createBind = useCallback(async () => {
        var title;
        var num = 1;
        do {
            title = `Шаблон №${num}`;
            num++;
        } while (binds.some(bind => bind.title === title));
        var auth_token = Cookies.get('auth_token');
        var response = await fetch('/api/app/addspamtemplate?title=' + title, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': auth_token
            },
        });
        if (response.redirected) {
            window.location.href = response.url;
        }
        var result = await response.json();
        if (result.status !== "ok") {
            showNotification(result.message, "error");
            return;
        }
        await getBinds();
        setSelectedBind({
            title,
            messages: [],
            delay: 1,
            threads: 1,
            mode: "random"
        });
    });

    return (
        <div className={styles.dropdown_menu} ref={dropdownRef}>
            <button className={classNames(
                styles.toggle_dropdown_button,
                isOpen && styles.open
            )} onClick={toggleDropdown}>
                <span>{selectedBind.title ? selectedBind.title : "Шаблон не выбран"}</span>
                <div>
                    <svg xmlns="http://www.w3.org/2000/svg" width="8" height="14" viewBox="0 0 8 14" fill="none">
                        <path d="M7 8.5L4 11.5L1 8.5L0 9.5L4 13.5L8 9.5L7 8.5Z" fill="#B9B9BA" />
                        <path d="M1 5.5L4 2.5L7 5.5L8 4.5L4 0.5L0 4.5L1 5.5Z" fill="#B9B9BA" />
                    </svg>
                </div>
            </button>
            {
                isRendered &&
                <div ref={dropdownContentRef} className={classNames(
                    styles.dropdown_content,
                    isOpen ? styles.open : styles.closed
                )} onClick={toggleDropdown}>
                    <div className={classNames(
                        _styles.list_container,
                    )}>
                        <div className={styles.binds_list}>
                            {
                                binds.map((item) => {
                                    return (
                                        <button className={
                                            classNames(
                                                selectedBind.title === item.title && styles.selected,
                                                styles.item
                                            )
                                        } key={item.title} onClick={() => setSelectedBind(item)}>{item.title}</button>
                                    )
                                })
                            }
                        </div>
                    </div>
                    <span className={styles.separator}></span>
                    <button onClick={createBind}>
                        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16" fill="none">
                            <g clipPath="url(#clip0_954_2490)">
                                <path d="M16.4863 9.55505L16.4863 6.44378L9.4859 6.51449L9.55661 -0.485869H6.44534L6.51605 6.51449L-0.484305 6.44378L-0.484305 9.55505L6.51605 9.48434L6.44534 16.4847H9.55661L9.4859 9.48434L16.4863 9.55505Z" fill="white" />
                            </g>
                            <defs>
                                <clipPath id="clip0_954_2490">
                                    <rect width="16" height="16" fill="white" />
                                </clipPath>
                            </defs>
                        </svg>
                    </button>
                </div>
            }
        </div>
    )
}

export default BindsTemplateDropdown;