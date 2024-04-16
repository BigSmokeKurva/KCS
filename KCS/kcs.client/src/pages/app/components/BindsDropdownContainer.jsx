import { useState, useRef, useEffect, useContext } from "react";
import styles from "../../shared_components/DropdownContainer.module.css";
import _styles from "./BindsDropdownContainer.module.css";
import classNames from "classnames";
import { BindsEditorContext } from "../contexts/BindsEditorContext";

function BindsDropdownContainer({ children, title, _isOpen, disabled, buttonStyle, callbackFunc }) {
    const [isOpen, setIsOpen] = useState(_isOpen !== undefined ? _isOpen : false);
    const containerRef = useRef(null);
    const buttonRef = useRef(null);
    const contentRef = useRef(null);
    const { openEditor } = useContext(BindsEditorContext);

    useEffect(() => {
        if (isOpen) {
            containerRef.current.style.removeProperty("height");
        }
    }, [isOpen])

    useEffect(() => {
        if (!isOpen && !disabled) {
            containerRef.current.style.height = buttonRef.current.offsetHeight + "px";
        }
    }, [])

    function openOptions() {
        openEditor({
            _callback: callbackFunc
        })
    }

    return (
        disabled ? (
            <div
                className={classNames(styles.dropdown_container, _styles.dropdown_container, !isOpen && styles.closed)}
                ref={containerRef}
            >
                <div ref={buttonRef} style={buttonStyle && buttonStyle} disabled={disabled}>
                    <span>{title}</span>
                    <button className={_styles.binds_options_button} onClick={openOptions}>
                        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="17" viewBox="0 0 16 17" fill="none">
                            <path fillRule="evenodd" clipRule="evenodd" d="M8 4.60999C9.1 4.60999 10 3.70999 10 2.60999C10 1.50999 9.1 0.609985 8 0.609985C6.9 0.609985 6 1.50999 6 2.60999C6 3.70999 6.9 4.60999 8 4.60999ZM8 6.60999C6.9 6.60999 6 7.50999 6 8.60999C6 9.70999 6.9 10.61 8 10.61C9.1 10.61 10 9.70999 10 8.60999C10 7.50999 9.1 6.60999 8 6.60999ZM6 14.61C6 13.51 6.9 12.61 8 12.61C9.1 12.61 10 13.51 10 14.61C10 15.71 9.1 16.61 8 16.61C6.9 16.61 6 15.71 6 14.61Z" fill="#EFEFF1" />
                        </svg>
                    </button>
                    <svg xmlns="http://www.w3.org/2000/svg" width="21" height="21" viewBox="0 0 21 21" fill="none">
                        <path d="M15 14.1001L10.5 9.6001L6 14.1001L4.5 12.6001L10.5 6.6001L16.5 12.6001L15 14.1001Z" fill="#EFEFF1" />
                    </svg>
                </div>
                {children}
            </div>
        ) : (
            <div
            className={classNames(styles.dropdown_container, _styles.dropdown_container, !isOpen && _styles.closed)}
                ref={containerRef}
                onTransitionEnd={() => {
                    if (!isOpen) {
                        containerRef.current.style.height = buttonRef.current.offsetHeight + "px";
                    }
                }}
            >
                <div ref={buttonRef} onClick={() => setIsOpen(!isOpen)}>
                    <span>{title}</span>
                    <button className={_styles.binds_options_button} onClick={openOptions}>
                        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="17" viewBox="0 0 16 17" fill="none">
                            <path fillRule="evenodd" clipRule="evenodd" d="M8 4.60999C9.1 4.60999 10 3.70999 10 2.60999C10 1.50999 9.1 0.609985 8 0.609985C6.9 0.609985 6 1.50999 6 2.60999C6 3.70999 6.9 4.60999 8 4.60999ZM8 6.60999C6.9 6.60999 6 7.50999 6 8.60999C6 9.70999 6.9 10.61 8 10.61C9.1 10.61 10 9.70999 10 8.60999C10 7.50999 9.1 6.60999 8 6.60999ZM6 14.61C6 13.51 6.9 12.61 8 12.61C9.1 12.61 10 13.51 10 14.61C10 15.71 9.1 16.61 8 16.61C6.9 16.61 6 15.71 6 14.61Z" fill="#EFEFF1" />
                        </svg>
                    </button>
                    <svg xmlns="http://www.w3.org/2000/svg" width="21" height="21" viewBox="0 0 21 21" fill="none">
                        <path d="M15 14.1001L10.5 9.6001L6 14.1001L4.5 12.6001L10.5 6.6001L16.5 12.6001L15 14.1001Z" fill="#EFEFF1" />
                    </svg>
                </div>
                <div ref={contentRef}>{children}</div>
            </div>
        )
    );
}

export default BindsDropdownContainer;
