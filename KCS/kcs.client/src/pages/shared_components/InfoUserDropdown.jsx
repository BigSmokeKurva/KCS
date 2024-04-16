import { useState, useRef, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import styles from "./InfoUserDropdown.module.css";
import classNames from "classnames";
import Cookies from "js-cookie";

function InfoUserDropdown({ username }) {
    const [isOpen, setIsOpen] = useState(false);
    const [isRendered, setIsRendered] = useState(false);
    const dropdownRef = useRef(null);
    const dropdownContentRef = useRef(null);
    const navigate = useNavigate();

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

    function rickroll() {
        const urls = [
            "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            "https://www.youtube.com/watch?v=nqVFhLZHptA"
        ];
        const randomIndex = Math.floor(Math.random() * urls.length);
        const url = urls[randomIndex];
        window.open(url, '_blank');
    }

    async function logout(){
        await fetch("/api/auth/logout", {
            method: "DELETE",
            headers: {
                "Content-Type": "application/json",
            }
        });
        Cookies.remove("auth_token");
        navigate("/signin");
    }

    useEffect(() => {
        window.addEventListener('click', handleClickOutside);
        return () => {
            window.removeEventListener('click', handleClickOutside);
        };
    }, []);

    return (
        <div className={styles.dropdown_menu} ref={dropdownRef}>
            <button className={classNames(
                styles.toggle_dropdown_button,
                isOpen && styles.open
            )} onClick={toggleDropdown}>
                <svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 48 48" fill="none">
                    <circle cx="24" cy="24" r="24" fill="#330C6E" />
                </svg>
                <span>{username && username[0]}</span>
            </button>
            {
                isRendered &&
                <div ref={dropdownContentRef} className={classNames(
                    styles.dropdown_content,
                    isOpen ? styles.open : styles.closed
                )} onClick={toggleDropdown}>
                    <button className={styles.username} onClick={rickroll}>{username}</button>
                    <div className={styles.separator}></div>
                    <button className={styles.exit} onClick={logout}>
                        <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 20 20" fill="none">
                            <g clipPath="url(#clip0_653_1069)">
                                <path d="M9.99984 2.08325C10.3314 2.08325 10.6493 2.21495 10.8837 2.44937C11.1181 2.68379 11.2498 3.00173 11.2498 3.33325C11.2498 3.66477 11.1181 3.98272 10.8837 4.21714C10.6493 4.45156 10.3314 4.58325 9.99984 4.58325H5.83317C5.72266 4.58325 5.61668 4.62715 5.53854 4.70529C5.4604 4.78343 5.4165 4.88941 5.4165 4.99992V14.9999C5.4165 15.1104 5.4604 15.2164 5.53854 15.2945C5.61668 15.3727 5.72266 15.4166 5.83317 15.4166H9.58317C9.91469 15.4166 10.2326 15.5483 10.4671 15.7827C10.7015 16.0171 10.8332 16.3351 10.8332 16.6666C10.8332 16.9981 10.7015 17.316 10.4671 17.5505C10.2326 17.7849 9.91469 17.9166 9.58317 17.9166H5.83317C5.05962 17.9166 4.31776 17.6093 3.77078 17.0623C3.22379 16.5153 2.9165 15.7735 2.9165 14.9999V4.99992C2.9165 4.22637 3.22379 3.4845 3.77078 2.93752C4.31776 2.39054 5.05962 2.08325 5.83317 2.08325H9.99984ZM15.0498 6.75825L17.4073 9.11658C17.6414 9.35096 17.7729 9.66867 17.7729 9.99992C17.7729 10.3312 17.6414 10.6489 17.4073 10.8833L15.0507 13.2416C14.8162 13.4761 14.4981 13.6078 14.1665 13.6078C13.8349 13.6078 13.5168 13.4761 13.2823 13.2416C13.0478 13.0071 12.9161 12.689 12.9161 12.3574C12.9161 12.0258 13.0478 11.7077 13.2823 11.4733L13.5057 11.2499H9.99984C9.66832 11.2499 9.35037 11.1182 9.11595 10.8838C8.88153 10.6494 8.74984 10.3314 8.74984 9.99992C8.74984 9.6684 8.88153 9.35046 9.11595 9.11603C9.35037 8.88161 9.66832 8.74992 9.99984 8.74992H13.5057L13.2823 8.52659C13.1663 8.41047 13.0742 8.27264 13.0114 8.12096C12.9487 7.96927 12.9164 7.80671 12.9164 7.64254C12.9164 7.47837 12.9488 7.31582 13.0117 7.16417C13.0745 7.01251 13.1666 6.87472 13.2828 6.75867C13.3989 6.64261 13.5367 6.55056 13.6884 6.48778C13.8401 6.42499 14.0026 6.39269 14.1668 6.39273C14.331 6.39277 14.4935 6.42514 14.6452 6.488C14.7968 6.55086 14.9346 6.64297 15.0507 6.75909L15.0498 6.75825Z" fill="#C4314B" />
                            </g>
                            <defs>
                                <clipPath id="clip0_653_1069">
                                    <rect width="20" height="20" fill="white" />
                                </clipPath>
                            </defs>
                        </svg>
                        Выйти
                    </button>
                </div>
            }
        </div>
    )
}

export default InfoUserDropdown;