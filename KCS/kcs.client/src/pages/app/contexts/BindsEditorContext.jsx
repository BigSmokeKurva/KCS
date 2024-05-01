import styles from "./BindsEditorContext.module.css";
import _styles from "../../admin_panel/style.module.css";
import classNames from "classnames";
import { createContext, useState, useContext, useEffect, useRef, useCallback } from "react";
import Cookies from "js-cookie";
import BindsTemplateDropdown from "./components/BindsTemplateDropdown";
import { NotificationsContext } from "../../../contexts/notification/NotificationsContext";

const keyNames = {
    AltLeft: 'ALT',
    AltRight: 'ALT',
    ArrowDown: '↓',
    ArrowLeft: '←',
    ArrowRight: '→',
    ArrowUp: '↑',
    Backquote: '`',
    Backslash: '\\',
    Backspace: 'BACKSPACE',
    BracketLeft: '[',
    BracketRight: ']',
    CapsLock: 'CAPS LOCK',
    Comma: ',',
    ControlLeft: 'CTRL',
    ControlRight: 'CTRL',
    Delete: 'DEL',
    Digit0: '0',
    Digit1: '1',
    Digit2: '2',
    Digit3: '3',
    Digit4: '4',
    Digit5: '5',
    Digit6: '6',
    Digit7: '7',
    Digit8: '8',
    Digit9: '9',
    End: 'END',
    Enter: 'ENTER',
    Equal: '=',
    Escape: 'ESC',
    F1: 'F1',
    F10: 'F10',
    F11: 'F11',
    F12: 'F12',
    F2: 'F2',
    F3: 'F3',
    F4: 'F4',
    F5: 'F5',
    F6: 'F6',
    F7: 'F7',
    F8: 'F8',
    F9: 'F9',
    Home: 'HOME',
    Insert: 'INSERT',
    KeyA: 'A',
    KeyB: 'B',
    KeyC: 'C',
    KeyD: 'D',
    KeyE: 'E',
    KeyF: 'F',
    KeyG: 'G',
    KeyH: 'H',
    KeyI: 'I',
    KeyJ: 'J',
    KeyK: 'K',
    KeyL: 'L',
    KeyM: 'M',
    KeyN: 'N',
    KeyO: 'O',
    KeyP: 'P',
    KeyQ: 'Q',
    KeyR: 'R',
    KeyS: 'S',
    KeyT: 'T',
    KeyU: 'U',
    KeyV: 'V',
    KeyW: 'W',
    KeyX: 'X',
    KeyY: 'Y',
    KeyZ: 'Z',
    MetaLeft: 'CMD',
    MetaRight: 'CMD',
    Minus: '-',
    Numpad0: '0',
    Numpad1: '1',
    Numpad2: '2',
    Numpad3: '3',
    Numpad4: '4',
    Numpad5: '5',
    Numpad6: '6',
    Numpad7: '7',
    Numpad8: '8',
    Numpad9: '9',
    NumpadAdd: '+',
    NumpadDecimal: '.',
    NumpadDivide: '/',
    NumpadEnter: 'ENTER',
    NumpadMultiply: '*',
    NumpadSubtract: '-',
    PageDown: 'PGDN',
    PageUp: 'PGUP',
    Period: '.',
    Quote: '\'',
    Semicolon: ';',
    ShiftLeft: 'SHIFT',
    ShiftRight: 'SHIFT',
    Slash: '/',
    Space: 'SPACE',
    Tab: 'TAB'
};

const BindsEditorContext = createContext();

function BindsEditorProvider({ children }) {
    const [isOpen, setIsOpen] = useState(false);
    const [binds, setBinds] = useState([]);
    const [selectedBind, setSelectedBind] = useState({});
    const containerRef = useRef(null);
    const textareaRef = useRef(null);
    const [titleInput, setTitleInput] = useState("");
    const [keys, setKeys] = useState([]);
    const [hotKeysIsEditing, setHotKeysIsEditing] = useState(false);
    const { showNotification } = useContext(NotificationsContext);
    const [callback, setCallback] = useState(null);

    const getBinds = useCallback(async () => {
        var auth_token = Cookies.get("auth_token");
        const response = await fetch("/api/app/getbinds", {
            method: "GET",
            headers: {
                "Content-Type": "application/json",
                "Authorization": auth_token,
            }
        });
        if (response.redirected) {
            window.location.href = response.url;
        }
        const data = await response.json();
        setBinds(data);
        if (selectedBind.title == undefined && data[0]) {
            setSelectedBind(data[0]);
        }
    });

    useEffect(() => {
        if (!isOpen) return;
        getBinds();
    }, [isOpen]);

    useEffect(() => {
        if (!textareaRef.current) return;
        if (selectedBind.title == undefined) {
            textareaRef.current.value = "";
            setTitleInput("");
            return;
        }
        textareaRef.current.value = selectedBind.messages.join(", ");
        textareaRef.current.focus();
        setTitleInput(selectedBind.title);
    }, [selectedBind]);

    function openEditor({ _callback }) {
        setIsOpen(true);
        setCallback(() => _callback);
        setTimeout(() => {
            containerRef.current.classList.add(styles.open);
        }, 30);
    }

    function handleClickOutside(event) {
        setHotKeysIsEditing(false);
        selectedBind.hotKeys = keys.length === 0 ? null : keys;
    }

    function closeEditor() {
        containerRef.current.classList.remove(styles.open);
        containerRef.current.classList.add(styles.close);
        containerRef.current.addEventListener('transitionend', () => {
            setIsOpen(false);
            setBinds([]);
            setSelectedBind({});
            setKeys([]);
            callback();
            setCallback(null);
        });
    }

    const updateBind = useCallback(async () => {
        var bind = {
            title: titleInput,
            hotKeys: selectedBind.hotKeys,
            messages: textareaRef.current.value.split(",").map((x) => x.trim())
        };
        const auth_token = Cookies.get("auth_token");
        var response = await fetch("/api/app/editbind", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Authorization": auth_token,
            },
            body: JSON.stringify({
                oldName: selectedBind.title,
                name: bind.title,
                messages: bind.messages,
                hotKeys: bind.hotKeys
            }),
        });
        if (response.redirected) {
            window.location.href = response.url;
        }
        var result = await response.json();
        if (result.status !== "ok") {
            showNotification(result.message, "error");
            return;
        }
        closeEditor();
        // TODO callback
    });

    const deleteBind = useCallback(async () => {
        const auth_token = Cookies.get("auth_token");
        var response = await fetch("/api/app/deletebind", {
            method: "DELETE",
            headers: {
                "Content-Type": "application/json",
                "Authorization": auth_token,
            },
            body: JSON.stringify({
                bindname: selectedBind.title,
            }),
        });
        if (response.redirected) {
            window.location.href = response.url;
        }
        var result = await response.json();
        if (result.status !== "ok") {
            showNotification(result.message, "error");
            return;
        }
        setSelectedBind({});
        await getBinds();
    });

    function startEditingHotKeys() {
        setHotKeysIsEditing(true);
        selectedBind.hotKeys = [];
        setKeys([]);
    }

    function deleteHotKeys() {
        selectedBind.hotKeys = null;
        setKeys([]);
    }

    const handleKeyDown = (event) => {
        if (!keys.includes(event.code)) {
            setKeys([...keys, event.code]);
        }
        event.preventDefault();
    };

    const handleKeyUp = (event) => {
        if (keys.length === 0) return;
        if (keys[0] === event.code) {
            setHotKeysIsEditing(false);
            selectedBind.hotKeys = keys;
            return;
        }
        setKeys(keys.filter((key) => key !== event.code));
        event.preventDefault();
    };

    return (
        <BindsEditorContext.Provider value={{ openEditor }}>
            {
                isOpen && (
                    <div
                        className={classNames(styles.binds_editor_container)}
                        ref={containerRef}
                        onKeyDown={hotKeysIsEditing ? handleKeyDown : null}
                        onKeyUp={hotKeysIsEditing ? handleKeyUp : null}
                        onClick={hotKeysIsEditing ? handleClickOutside : null}
                    >
                        <div className={styles.overlay}></div>
                        <div className={styles.editor}>
                            <div className={styles.header}>
                                <span>Редактор биндов</span>
                                <button className={styles.close_button} onClick={closeEditor}>
                                    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 14 14" fill="none">
                                        <path d="M14 2.16901L11.831 0L7 4.92958L2.16901 0L0 2.16901L4.92958 7L0 11.831L2.16901 14L7 9.07042L11.831 14L14 11.831L9.07042 7L14 2.16901Z" fill="white" />
                                    </svg>
                                </button>
                            </div>
                            <div className={styles.template_container}>
                                <div className={styles.template_control}>
                                    <span className={styles.title}>Шаблон</span>
                                    <div>
                                        <BindsTemplateDropdown binds={binds} getBinds={getBinds} selectedBind={selectedBind} setSelectedBind={setSelectedBind} />
                                        <button onClick={deleteBind} className={styles.delete_template_button} disabled={selectedBind.title == undefined}>
                                            {
                                                selectedBind.title ?
                                                    <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none">
                                                        <g clipPath="url(#clip0_759_442)">
                                                            <path d="M14.28 2C14.6998 2.00011 15.1088 2.13229 15.4493 2.37781C15.7898 2.62333 16.0444 2.96975 16.177 3.368L16.72 5H20C20.2652 5 20.5196 5.10536 20.7071 5.29289C20.8946 5.48043 21 5.73478 21 6C21 6.26522 20.8946 6.51957 20.7071 6.70711C20.5196 6.89464 20.2652 7 20 7L19.997 7.071L19.13 19.214C19.0759 19.9706 18.7372 20.6786 18.182 21.1956C17.6269 21.7125 16.8965 21.9999 16.138 22H7.862C7.10346 21.9999 6.37311 21.7125 5.81797 21.1956C5.26283 20.6786 4.92411 19.9706 4.87 19.214L4.003 7.07C4.00119 7.04671 4.00019 7.02336 4 7C3.73478 7 3.48043 6.89464 3.29289 6.70711C3.10536 6.51957 3 6.26522 3 6C3 5.73478 3.10536 5.48043 3.29289 5.29289C3.48043 5.10536 3.73478 5 4 5H7.28L7.823 3.368C7.9557 2.96959 8.21043 2.62305 8.5511 2.37752C8.89176 2.13198 9.30107 1.9999 9.721 2H14.28ZM17.997 7H6.003L6.865 19.071C6.88295 19.3232 6.99577 19.5592 7.18076 19.7316C7.36574 19.904 7.60916 19.9999 7.862 20H16.138C16.3908 19.9999 16.6343 19.904 16.8192 19.7316C17.0042 19.5592 17.117 19.3232 17.135 19.071L17.997 7ZM10 10C10.2449 10 10.4813 10.09 10.6644 10.2527C10.8474 10.4155 10.9643 10.6397 10.993 10.883L11 11V16C10.9997 16.2549 10.9021 16.5 10.7272 16.6854C10.5522 16.8707 10.313 16.9822 10.0586 16.9972C9.80416 17.0121 9.55362 16.9293 9.35817 16.7657C9.16271 16.6021 9.0371 16.3701 9.007 16.117L9 16V11C9 10.7348 9.10536 10.4804 9.29289 10.2929C9.48043 10.1054 9.73478 10 10 10ZM14 10C14.2652 10 14.5196 10.1054 14.7071 10.2929C14.8946 10.4804 15 10.7348 15 11V16C15 16.2652 14.8946 16.5196 14.7071 16.7071C14.5196 16.8946 14.2652 17 14 17C13.7348 17 13.4804 16.8946 13.2929 16.7071C13.1054 16.5196 13 16.2652 13 16V11C13 10.7348 13.1054 10.4804 13.2929 10.2929C13.4804 10.1054 13.7348 10 14 10ZM14.28 4H9.72L9.387 5H14.613L14.28 4Z" fill="#C4314B" />
                                                        </g>
                                                        <defs>
                                                            <clipPath id="clip0_759_442">
                                                                <rect width="24" height="24" fill="white" />
                                                            </clipPath>
                                                        </defs>
                                                    </svg> :
                                                    <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none">
                                                        <g clipPath="url(#clip0_740_9079)">
                                                            <path d="M14.28 2C14.6998 2.00011 15.1088 2.13229 15.4493 2.37781C15.7898 2.62333 16.0444 2.96975 16.177 3.368L16.72 5H20C20.2652 5 20.5196 5.10536 20.7071 5.29289C20.8946 5.48043 21 5.73478 21 6C21 6.26522 20.8946 6.51957 20.7071 6.70711C20.5196 6.89464 20.2652 7 20 7L19.997 7.071L19.13 19.214C19.0759 19.9706 18.7372 20.6786 18.182 21.1956C17.6269 21.7125 16.8965 21.9999 16.138 22H7.862C7.10346 21.9999 6.37311 21.7125 5.81797 21.1956C5.26283 20.6786 4.92411 19.9706 4.87 19.214L4.003 7.07C4.00119 7.04671 4.00019 7.02336 4 7C3.73478 7 3.48043 6.89464 3.29289 6.70711C3.10536 6.51957 3 6.26522 3 6C3 5.73478 3.10536 5.48043 3.29289 5.29289C3.48043 5.10536 3.73478 5 4 5H7.28L7.823 3.368C7.9557 2.96959 8.21043 2.62305 8.5511 2.37752C8.89176 2.13198 9.30107 1.9999 9.721 2H14.28ZM17.997 7H6.003L6.865 19.071C6.88295 19.3232 6.99577 19.5592 7.18076 19.7316C7.36574 19.904 7.60916 19.9999 7.862 20H16.138C16.3908 19.9999 16.6343 19.904 16.8192 19.7316C17.0042 19.5592 17.117 19.3232 17.135 19.071L17.997 7ZM10 10C10.2449 10 10.4813 10.09 10.6644 10.2527C10.8474 10.4155 10.9643 10.6397 10.993 10.883L11 11V16C10.9997 16.2549 10.9021 16.5 10.7272 16.6854C10.5522 16.8707 10.313 16.9822 10.0586 16.9972C9.80416 17.0121 9.55362 16.9293 9.35817 16.7657C9.16271 16.6021 9.0371 16.3701 9.007 16.117L9 16V11C9 10.7348 9.10536 10.4804 9.29289 10.2929C9.48043 10.1054 9.73478 10 10 10ZM14 10C14.2652 10 14.5196 10.1054 14.7071 10.2929C14.8946 10.4804 15 10.7348 15 11V16C15 16.2652 14.8946 16.5196 14.7071 16.7071C14.5196 16.8946 14.2652 17 14 17C13.7348 17 13.4804 16.8946 13.2929 16.7071C13.1054 16.5196 13 16.2652 13 16V11C13 10.7348 13.1054 10.4804 13.2929 10.2929C13.4804 10.1054 13.7348 10 14 10ZM14.28 4H9.72L9.387 5H14.613L14.28 4Z" fill="#3F4246" />
                                                        </g>
                                                        <defs>
                                                            <clipPath id="clip0_740_9079">
                                                                <rect width="24" height="24" fill="white" />
                                                            </clipPath>
                                                        </defs>
                                                    </svg>
                                            }
                                        </button>
                                    </div>
                                </div>
                                <div className={styles.template_title}>
                                    <span className={styles.title}>Название</span>
                                    <input
                                        type="text"
                                        className={styles.title_bind}
                                        disabled={selectedBind.title == undefined}
                                        value={titleInput}
                                        onChange={(e) => setTitleInput(e.target.value)}
                                    />
                                </div>
                            </div>
                            <div className={styles.hotkey_container}>
                                <span className={styles.title}>Горячие клавиши</span>
                                <span className={styles.hotkeys_title}>{
                                    selectedBind.title && (
                                        hotKeysIsEditing && keys.length !== 0 ?
                                            keys.map((key) => keyNames[key]).join(" + ") :
                                            hotKeysIsEditing && keys.length === 0 ?
                                                "Нажмите клавиши" :
                                                !selectedBind.hotKeys ?
                                                    "Не назначены" :
                                                    selectedBind.hotKeys.map((key) => keyNames[key]).join(" + "))
                                }</span>
                                <button
                                    className={classNames(
                                        _styles.create_invite_code_button,
                                        styles.hotkey_button
                                    )}
                                    disabled={selectedBind.title == undefined}
                                    onClick={hotKeysIsEditing ? null : selectedBind.hotKeys != null ? deleteHotKeys : startEditingHotKeys}
                                >
                                    {
                                        hotKeysIsEditing ?
                                            "Ожидание" :
                                            !selectedBind.hotKeys ?
                                                "Назначить" :
                                                "Удалить"
                                    }
                                </button>
                            </div>
                            <div className={styles.messages}>
                                <span className={styles.title}>Сообщения</span>
                                <textarea disabled={selectedBind.title == undefined} ref={textareaRef} className={styles.textarea}></textarea>
                            </div>
                            <div className={styles.buttons_container}>
                                <button className={classNames(
                                    _styles.invite_code_cancel_button,
                                    _styles.filter_button
                                )} onClick={closeEditor}>Отмена</button>
                                <button className={_styles.create_invite_code_button} onClick={selectedBind.title ? updateBind : closeEditor}>Сохранить</button>
                            </div>
                        </div>
                    </div>
                )
            }
            {children}
        </BindsEditorContext.Provider>
    );
}

export { BindsEditorContext, BindsEditorProvider }