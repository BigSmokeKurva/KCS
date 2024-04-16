import { useState, useRef, useEffect, forwardRef, useImperativeHandle } from "react";
import styles from "./EditStreamerUsername.module.css";

const EditStreamerUsername = forwardRef(({ callbackFunc, placeholder }, ref) => {
    const [isEdit, setIsEdit] = useState(false);
    const [inputValue, setInputValue] = useState("");
    const inputRef = useRef(null);

    useImperativeHandle(ref, () => ({
        getInputValue: () => {
            return inputValue;
        },
        setInputValue: (value) => {
            setInputValue(value);
        }
    }));

    useEffect(() => {
        if (isEdit) {
            inputRef.current.setSelectionRange(inputValue.length, inputValue.length);
            inputRef.current.focus();
        }
    }, [isEdit]);

    return (
        <div className={styles.container}>
            <div className={styles.field_container}>
                <input
                    ref={inputRef}
                    type="text"
                    className={styles.input}
                    placeholder={placeholder}
                    disabled={!isEdit}
                    value={inputValue}
                    onChange={(e) => setInputValue(e.target.value)}
                    onKeyUp={(e) => {
                        if (e.key === "Enter") {
                            setIsEdit(false);
                            callbackFunc();
                        }
                    }}
                />
                <button className={styles.edit_button} onClick={isEdit ? () => {
                    setIsEdit(false);
                    callbackFunc();
                } : setIsEdit}>
                    {
                        isEdit ?
                            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 20 20" fill="none">
                                <path d="M2.5 4.27778C2.5 3.80628 2.6873 3.3541 3.0207 3.0207C3.3541 2.6873 3.80628 2.5 4.27778 2.5H14.5764C15.0479 2.5001 15.5 2.68747 15.8333 3.02089L18.2396 5.42711C18.4063 5.59377 18.4999 5.81983 18.5 6.05556V16.7222C18.5 17.1937 18.3127 17.6459 17.9793 17.9793C17.6459 18.3127 17.1937 18.5 16.7222 18.5H4.27778C3.80628 18.5 3.3541 18.3127 3.0207 17.9793C2.6873 17.6459 2.5 17.1937 2.5 16.7222V4.27778ZM7.83333 16.7222H13.1667V11.3889H7.83333V16.7222ZM14.9444 16.7222H16.7222V6.42356L14.9444 4.64578V6.05556C14.9444 6.52705 14.7571 6.97924 14.4237 7.31263C14.0903 7.64603 13.6382 7.83333 13.1667 7.83333H7.83333C7.36184 7.83333 6.90965 7.64603 6.57625 7.31263C6.24286 6.97924 6.05556 6.52705 6.05556 6.05556V4.27778H4.27778V16.7222H6.05556V11.3889C6.05556 10.9174 6.24286 10.4652 6.57625 10.1318C6.90965 9.79841 7.36184 9.61111 7.83333 9.61111H13.1667C13.6382 9.61111 14.0903 9.79841 14.4237 10.1318C14.7571 10.4652 14.9444 10.9174 14.9444 11.3889V16.7222ZM7.83333 4.27778V6.05556H13.1667V4.27778H7.83333Z" fill="white" />
                            </svg> :
                            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 20 20" fill="none">
                                <path d="M17.9805 5.51097L14.4891 2.02035C14.1961 1.72742 13.7987 1.56287 13.3844 1.56287C12.9701 1.56287 12.5727 1.72742 12.2797 2.02035L2.64532 11.6539C2.49973 11.7986 2.3843 11.9708 2.30571 12.1604C2.22713 12.35 2.18695 12.5534 2.18751 12.7586V16.25C2.18751 16.6644 2.35213 17.0619 2.64515 17.3549C2.93818 17.6479 3.3356 17.8125 3.75001 17.8125H7.24141C7.44668 17.8131 7.65003 17.773 7.83966 17.6944C8.02929 17.6158 8.20143 17.5003 8.3461 17.3547L14.5539 11.1477L14.8789 12.2328L12.15 14.9618C11.9739 15.1379 11.8749 15.3767 11.8749 15.6258C11.8749 15.8749 11.9739 16.1138 12.15 16.2899C12.3261 16.466 12.565 16.5649 12.8141 16.5649C13.0631 16.5649 13.302 16.466 13.4781 16.2899L16.6031 13.1649C16.7228 13.0451 16.8078 12.8951 16.8491 12.7308C16.8903 12.5666 16.8863 12.3942 16.8375 12.2321L16.0609 9.64378L17.982 7.72269C18.1273 7.57746 18.2426 7.405 18.3211 7.21519C18.3997 7.02538 18.4401 6.82193 18.4399 6.61651C18.4398 6.41108 18.3991 6.20769 18.3203 6.01799C18.2414 5.82829 18.126 5.656 17.9805 5.51097ZM5.07813 11.875L10.625 6.32816L13.6719 9.37503L8.12501 14.9219L5.07813 11.875ZM4.06251 13.5157L6.48438 15.9375H4.06251V13.5157ZM15 8.04691L11.9531 5.00003L13.3859 3.56722L16.4328 6.6141L15 8.04691Z" fill="#EFEFF1" />
                            </svg>
                    }
                </button>
            </div>
        </div>
    )
});

export default EditStreamerUsername;