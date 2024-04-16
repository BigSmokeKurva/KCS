import styles from './BotActionDropdown.module.css';
import _styles from "../style.module.css";
import classNames from 'classnames';
import { useState, useRef, useEffect } from 'react';

function BotActionDropdown({ bot, connectBot, disconnectBot, followBot, unfollowBot, removeFromQueue, isMobile }) {
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

    useEffect(() => {
        const scrollContainers = document.querySelectorAll("." + _styles.list_container);

        const handleScroll = () => toggleDropdown();

        if (isOpen) {
            const rect = dropdownRef.current.getBoundingClientRect();
            if (isMobile) {
                const parentNode = dropdownRef.current.parentNode;
                dropdownContentRef.current.style.left = parentNode.parentNode.getBoundingClientRect().width - dropdownContentRef.current.getBoundingClientRect().width + "px";
                var top = rect.top - parentNode.parentNode.parentNode.parentNode.parentNode.parentNode.getBoundingClientRect().top + rect.height;
                if (top + dropdownContentRef.current.offsetHeight > parentNode.parentNode.parentNode.parentNode.parentNode.parentNode.offsetHeight) {
                    top = rect.top - parentNode.parentNode.parentNode.parentNode.parentNode.parentNode.getBoundingClientRect().top - dropdownContentRef.current.offsetHeight;
                    dropdownContentRef.current.classList.add(styles.is_top);
                }

                dropdownContentRef.current.style.top = top + "px";
            } else {
                var top = rect.top + rect.height;
                if (top + dropdownContentRef.current.offsetHeight > window.innerHeight) {
                    top = rect.top - dropdownContentRef.current.offsetHeight;
                    dropdownContentRef.current.classList.add(styles.is_top);
                }

                dropdownContentRef.current.style.top = top + "px";
            }
            scrollContainers.forEach(container => container.addEventListener('scroll', handleScroll));
        }

        return () => {
            scrollContainers.forEach(container => container.removeEventListener('scroll', handleScroll));
        };
    }, [isOpen]);



    return (
        <div className={classNames(
            styles.dropdown_menu,
        )} ref={dropdownRef}>
            <button className={classNames(
                styles.toggle_dropdown_button,
                isOpen && styles.open,
            )} onClick={toggleDropdown}>
                <svg xmlns="http://www.w3.org/2000/svg" width="4" height="16" viewBox="0 0 4 16" fill="none">
                    <path fillRule="evenodd" clipRule="evenodd" d="M2 4C3.1 4 4 3.1 4 2C4 0.9 3.1 0 2 0C0.9 0 0 0.9 0 2C0 3.1 0.9 4 2 4ZM2 6C0.9 6 0 6.9 0 8C0 9.1 0.9 10 2 10C3.1 10 4 9.1 4 8C4 6.9 3.1 6 2 6ZM0 14C0 12.9 0.9 12 2 12C3.1 12 4 12.9 4 14C4 15.1 3.1 16 2 16C0.9 16 0 15.1 0 14Z" fill="white" fillOpacity="0.7" />
                </svg>
            </button>
            {
                isRendered &&
                <div ref={dropdownContentRef} className={classNames(
                    styles.dropdown_content,
                    isOpen ? styles.open : styles.closed,
                )} onClick={toggleDropdown}>
                    <button onClick={!bot.isConnected ? () => connectBot(bot) : () => disconnectBot(bot)}>
                        {
                            !bot.isConnected ? <>
                                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 20 20" fill="none">
                                    <path d="M10 0C10.918 0 11.8001 0.120443 12.6465 0.361328C13.4928 0.602214 14.2871 0.940755 15.0293 1.37695C15.7715 1.81315 16.4453 2.33724 17.0508 2.94922C17.6628 3.55469 18.1868 4.22852 18.623 4.9707C19.0592 5.71289 19.3978 6.50716 19.6387 7.35352C19.8796 8.19987 20 9.08203 20 10C20 10.9115 19.8796 11.7936 19.6387 12.6465C19.3978 13.4928 19.0592 14.2871 18.623 15.0293C18.1868 15.7715 17.6628 16.4486 17.0508 17.0605C16.4453 17.666 15.7715 18.1868 15.0293 18.623C14.2871 19.0592 13.4896 19.3978 12.6367 19.6387C11.7904 19.8796 10.9115 20 10 20C9.08854 20 8.20638 19.8796 7.35352 19.6387C6.50716 19.3978 5.71289 19.0592 4.9707 18.623C4.22852 18.1868 3.55143 17.666 2.93945 17.0605C2.33398 16.4486 1.81315 15.7715 1.37695 15.0293C0.940755 14.2871 0.602214 13.4928 0.361328 12.6465C0.120443 11.7936 0 10.9115 0 10C0 9.08854 0.120443 8.20964 0.361328 7.36328C0.602214 6.51042 0.940755 5.71289 1.37695 4.9707C1.81315 4.22852 2.33398 3.55469 2.93945 2.94922C3.55143 2.33724 4.22852 1.81315 4.9707 1.37695C5.71289 0.940755 6.50716 0.602214 7.35352 0.361328C8.20638 0.120443 9.08854 0 10 0ZM13.291 7.10938C13.0306 7.10938 12.8092 7.20052 12.627 7.38281L9.0918 10.9375L7.83203 9.6875C7.64974 9.50521 7.43164 9.41406 7.17773 9.41406C7.05404 9.41406 6.93685 9.4401 6.82617 9.49219C6.71549 9.54427 6.61458 9.61589 6.52344 9.70703C6.4388 9.79167 6.37044 9.88932 6.31836 10C6.26628 10.1107 6.24023 10.2279 6.24023 10.3516C6.24023 10.612 6.33138 10.8333 6.51367 11.0156L8.42773 12.9297C8.61003 13.112 8.83138 13.2031 9.0918 13.2031C9.35221 13.2031 9.57357 13.112 9.75586 12.9297L13.9551 8.71094C14.1374 8.52865 14.2285 8.30729 14.2285 8.04688C14.2285 7.92318 14.2025 7.80599 14.1504 7.69531C14.0983 7.58464 14.0267 7.48698 13.9355 7.40234C13.8509 7.3112 13.7533 7.23958 13.6426 7.1875C13.5319 7.13542 13.4147 7.10938 13.291 7.10938Z" fill="white" fillOpacity="0.7" />
                                </svg>
                                Подключить
                            </> :
                                <>
                                    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 20 20" fill="none">
                                        <path d="M10 0C10.918 0 11.8001 0.120443 12.6465 0.361328C13.4993 0.602214 14.2936 0.940755 15.0293 1.37695C15.7715 1.80664 16.4486 2.32747 17.0605 2.93945C17.6725 3.55143 18.1934 4.22852 18.623 4.9707C19.0592 5.70638 19.3978 6.50065 19.6387 7.35352C19.8796 8.19987 20 9.08203 20 10C20 10.9115 19.8796 11.7936 19.6387 12.6465C19.3978 13.4928 19.0592 14.2871 18.623 15.0293C18.1868 15.7715 17.6628 16.4486 17.0508 17.0605C16.4453 17.666 15.7715 18.1868 15.0293 18.623C14.2871 19.0592 13.4928 19.3978 12.6465 19.6387C11.8001 19.8796 10.918 20 10 20C9.08854 20 8.20638 19.8796 7.35352 19.6387C6.50716 19.3978 5.71289 19.0592 4.9707 18.623C4.22852 18.1868 3.55143 17.666 2.93945 17.0605C2.33398 16.4486 1.81315 15.7715 1.37695 15.0293C0.940755 14.2871 0.602214 13.4928 0.361328 12.6465C0.120443 11.7936 0 10.9115 0 10C0 9.08203 0.120443 8.19987 0.361328 7.35352C0.602214 6.50716 0.940755 5.71289 1.37695 4.9707C1.81315 4.22852 2.33398 3.55469 2.93945 2.94922C3.55143 2.33724 4.22852 1.81315 4.9707 1.37695C5.71289 0.940755 6.50716 0.602214 7.35352 0.361328C8.20638 0.120443 9.08854 0 10 0ZM6.8457 8.94531C6.70898 8.94531 6.57552 8.97461 6.44531 9.0332C6.32161 9.0918 6.21094 9.16992 6.11328 9.26758C6.01562 9.36523 5.9375 9.47917 5.87891 9.60938C5.82031 9.73307 5.79102 9.86328 5.79102 10C5.79102 10.1367 5.82031 10.2702 5.87891 10.4004C5.9375 10.5241 6.01562 10.6348 6.11328 10.7324C6.21094 10.8301 6.32161 10.9082 6.44531 10.9668C6.57552 11.0254 6.70898 11.0547 6.8457 11.0547H13.1543C13.291 11.0547 13.4212 11.0254 13.5449 10.9668C13.6751 10.9082 13.7891 10.8301 13.8867 10.7324C13.9844 10.6348 14.0625 10.5241 14.1211 10.4004C14.1797 10.2702 14.209 10.1367 14.209 10C14.209 9.86328 14.1797 9.73307 14.1211 9.60938C14.0625 9.47917 13.9844 9.36523 13.8867 9.26758C13.7891 9.16992 13.6751 9.0918 13.5449 9.0332C13.4212 8.97461 13.291 8.94531 13.1543 8.94531H6.8457Z" fill="white" fillOpacity="0.7" />
                                    </svg>
                                    Отключить
                                </>
                        }
                    </button>
                    {
                        bot.isQueue ? (
                            <button onClick={() => removeFromQueue(bot)} className={styles.remove_from_queue_button}>
                                <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 20 20" fill="none">
                                    <path d="M11.25 5.83333H17.5C17.721 5.83333 17.9329 5.92113 18.0892 6.07741C18.2455 6.23369 18.3333 6.44565 18.3333 6.66667V12.5C18.3333 12.721 18.2455 12.933 18.0892 13.0893C17.9329 13.2455 17.721 13.3333 17.5 13.3333H15.8333M4.58329 5.83333H2.49996C2.27895 5.83333 2.06698 5.92113 1.9107 6.07741C1.75442 6.23369 1.66663 6.44565 1.66663 6.66667V12.5C1.66663 12.721 1.75442 12.933 1.9107 13.0893C2.06698 13.2455 2.27895 13.3333 2.49996 13.3333H8.74996M5.83329 2.5L14.1666 16.6667M13.3333 9.58333H15M4.99996 9.58333H6.66663" stroke="#C4314B" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
                                </svg>
                                Убрать из очереди
                            </button>
                        ) : (
                            <button onClick={!bot.isFollowed ? () => followBot(bot) : () => unfollowBot(bot)}>
                                {
                                    !bot.isFollowed ?
                                        <>
                                            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 20 20" fill="none">
                                                <path d="M18.0273 5.53908C17.7655 4.9329 17.3881 4.38359 16.916 3.92189C16.4436 3.45881 15.8866 3.09082 15.2754 2.8379C14.6416 2.57461 13.9617 2.43984 13.2754 2.44142C12.3125 2.44142 11.373 2.70509 10.5566 3.20314C10.3613 3.32228 10.1758 3.45314 10 3.59572C9.82422 3.45314 9.63867 3.32228 9.44336 3.20314C8.62695 2.70509 7.6875 2.44142 6.72461 2.44142C6.03125 2.44142 5.35938 2.57423 4.72461 2.8379C4.11133 3.09181 3.55859 3.45705 3.08398 3.92189C2.61132 4.38306 2.23375 4.93251 1.97266 5.53908C1.70117 6.16994 1.5625 6.83986 1.5625 7.52931C1.5625 8.1797 1.69531 8.85744 1.95898 9.54689C2.17969 10.1231 2.49609 10.7207 2.90039 11.3242C3.54102 12.2793 4.42188 13.2754 5.51562 14.2852C7.32812 15.959 9.12305 17.1152 9.19922 17.1621L9.66211 17.459C9.86719 17.5899 10.1309 17.5899 10.3359 17.459L10.7988 17.1621C10.875 17.1133 12.668 15.959 14.4824 14.2852C15.5762 13.2754 16.457 12.2793 17.0977 11.3242C17.502 10.7207 17.8203 10.1231 18.0391 9.54689C18.3027 8.85744 18.4355 8.1797 18.4355 7.52931C18.4375 6.83986 18.2988 6.16994 18.0273 5.53908Z" fill="white" fillOpacity="0.7" />
                                            </svg>
                                            Подписать
                                        </> :
                                        <>
                                            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 20 20" fill="none">
                                                <path d="M18.7352 7.77342C18.3297 13.0406 10.6336 17.2445 10.2977 17.425C10.2066 17.4739 10.1049 17.4996 10.0016 17.4996C9.89822 17.4996 9.79648 17.4739 9.70547 17.425C9.35938 17.2398 1.25 12.8125 1.25 7.34373C1.24978 6.46629 1.48791 5.60526 1.93896 4.85262C2.39001 4.09998 3.03703 3.484 3.81091 3.07046C4.5848 2.65693 5.45648 2.46137 6.33286 2.50469C7.20923 2.548 8.05738 2.82856 8.78672 3.31639C8.82537 3.34201 8.85783 3.37592 8.88175 3.41565C8.90567 3.45538 8.92044 3.49994 8.925 3.54609C8.92957 3.59224 8.9238 3.63882 8.90813 3.68247C8.89245 3.72611 8.86726 3.76572 8.83438 3.79842L7.79063 4.84373C7.6735 4.96093 7.60771 5.11984 7.60771 5.28553C7.60771 5.45122 7.6735 5.61013 7.79063 5.72732L10.332 8.26873L8.67188 9.92186C8.6114 9.97931 8.56303 10.0483 8.52963 10.1247C8.49623 10.2012 8.47848 10.2836 8.47741 10.367C8.47634 10.4504 8.49198 10.5332 8.52341 10.6104C8.55484 10.6877 8.60143 10.7579 8.66042 10.8169C8.7194 10.8759 8.78961 10.9225 8.86688 10.9539C8.94416 10.9853 9.02694 11.001 9.11036 10.9999C9.19377 10.9988 9.27613 10.9811 9.35258 10.9477C9.42902 10.9143 9.49801 10.8659 9.55547 10.8054L11.6555 8.70623C11.7726 8.58903 11.8384 8.43012 11.8384 8.26443C11.8384 8.09874 11.7726 7.93983 11.6555 7.82264L9.11719 5.28357L10.482 3.91873C10.9345 3.4639 11.4734 3.10409 12.0669 2.86045C12.6605 2.61681 13.2967 2.49426 13.9383 2.49998C16.7758 2.51795 18.9523 4.94451 18.7352 7.77342Z" fill="white" fillOpacity="0.7" />
                                            </svg>
                                            Отписать
                                        </>
                                }
                            </button>
                        )
                    }
                </div>
            }
        </div>
    )
}

export default BotActionDropdown;