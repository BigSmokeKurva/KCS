import styles from "./FollowBotContext.module.css";
import _styles from "../../admin_panel/style.module.css";
import classNames from "classnames";
import { createContext, useState, useRef, useEffect } from "react";

const FollowBotContext = createContext();

function FollowBotProvider({ children }) {
    const [callbackFunc, setCallbackFunc] = useState(null);
    const containerRef = useRef(null);
    const inputRef = useRef(null);
    const [inputValue, setInputValue] = useState("0");

    function openFollowBotOptions(props) {
        setCallbackFunc(() => (delay) => props._callbackFunc(delay));
        setInputValue("0");
        setTimeout(() => {
            containerRef.current.classList.add(styles.open);
            inputRef.current.focus();
        }, 30);
        document.body.style.overflow = "hidden";
    }

    function closeOptions() {
        containerRef.current.classList.remove(styles.open);
        containerRef.current.classList.add(styles.close);
        containerRef.current.addEventListener('transitionend', () => {
            setCallbackFunc(null);
        });
        document.body.style.removeProperty("overflow");
    }

    function startButton(){
        callbackFunc(inputValue);
        setCallbackFunc(null);
        setInputValue("0");
    }

    return (
        <FollowBotContext.Provider value={{ openFollowBotOptions }}>
            {
                callbackFunc &&
                <>
                    <div className={styles.options_container} ref={containerRef}>
                        <div className={styles.overlay}></div>
                        <div className={styles.options}>
                            <div className={styles.title}>
                                <span>Настройки</span>
                                <button className={styles.close_button} onClick={closeOptions}>
                                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16" fill="none">
                                        <g clipPath="url(#clip0_759_488)">
                                            <path d="M15.1 3.0999L12.9 0.899902L8.00002 5.8999L3.10002 0.899902L0.900024 3.0999L5.90002 7.9999L0.900024 12.8999L3.10002 15.0999L8.00002 10.0999L12.9 15.0999L15.1 12.8999L10.1 7.9999L15.1 3.0999Z" fill="white" />
                                        </g>
                                        <defs>
                                            <clipPath id="clip0_759_488">
                                                <rect width="16" height="16" fill="white" />
                                            </clipPath>
                                        </defs>
                                    </svg>
                                </button>
                            </div>
                            <div className={styles.body}>
                                <span>Задержка</span>
                                <input type="text"
                                    value={inputValue}
                                    onChange={(e) => setInputValue(e.target.value.replace(/\D/g, ''))}
                                    ref={inputRef}
                                />
                                <span>секунд</span>
                            </div>
                            <div className={styles.buttons_container}>
                                <button className={classNames(
                                    _styles.invite_code_cancel_button,
                                    _styles.filter_button
                                )} onClick={closeOptions}>Отмена</button>
                                <button className={_styles.create_invite_code_button} onClick={startButton}>Запуск</button>
                            </div>
                        </div>
                    </div>
                </>
            }
            {children}
        </FollowBotContext.Provider>
    )
}

export { FollowBotContext, FollowBotProvider };