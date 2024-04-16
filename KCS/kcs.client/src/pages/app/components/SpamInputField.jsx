import styles from "./SpamInputField.module.css";
import { useRef } from "react";

function SpamInputField({ value, setValue, rightText, title, disabled }) {
    const containerRef = useRef(null);
    return (
        <div className={styles.container}>
            <div className={styles.input_container} disabled={disabled} ref={containerRef}>
                <div>
                    <input
                        className={styles.input}
                        type="text"
                        disabled={disabled}
                        value={value}
                        onChange={() => setValue(event.target.value)}
                        onFocus={() => {
                            containerRef.current.classList.add(styles.focus)
                        }}
                        onBlur={() => {
                            containerRef.current.classList.remove(styles.focus)
                        }}
                    />
                    {rightText && <span>{rightText}</span>}
                </div>
                <svg xmlns="http://www.w3.org/2000/svg" width="8" height="14" viewBox="0 0 8 14" fill="none">
                    <path d="M7 8.5L4 11.5L1 8.5L0 9.5L4 13.5L8 9.5L7 8.5Z" fill="#B9B9BA" />
                    <path d="M1 5.5L4 2.5L7 5.5L8 4.5L4 0.5L0 4.5L1 5.5Z" fill="#B9B9BA" />
                </svg>
            </div>
            <span className={styles.title} disabled={disabled}>{title}</span>
        </div>
    )
}

export default SpamInputField;