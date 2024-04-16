import { createContext, useState, useRef } from "react";
import styles from "./EditorContext.module.css";
import _styles from "../style.module.css";
import classNames from "classnames";

const EditorContext = createContext();

function EditorProvider({ children }) {
    const [editorData, setEditorData] = useState(null);
    const containerRef = useRef(null);
    const textareaRef = useRef(null);

    function openEditor({ _editorData }) {
        setEditorData(_editorData);
        setTimeout(() => {
            containerRef.current.classList.add(styles.open);
            textareaRef.current.value = _editorData.text
            textareaRef.current.focus();
        }, 30);
        document.body.style.overflow = "hidden";
    }

    function saveData() {
        editorData.callback(textareaRef.current.value);
        closeEditor();
    }

    function closeEditor() {
        containerRef.current.classList.remove(styles.open);
        containerRef.current.classList.add(styles.close);
        containerRef.current.addEventListener('transitionend', () => {
            setEditorData(null);
        });
        document.body.style.removeProperty("overflow");
    }

    return (
        <EditorContext.Provider value={{ openEditor }}>
            {
                editorData &&
                <>
                    <div ref={containerRef} className={classNames(
                        styles.editor_container,
                    )}>
                        <div className={styles.overlay}></div>
                        <div className={styles.editor}>
                            <div className={styles.header}>
                                <span>Редактор</span>
                                <button className={styles.close_button} onClick={closeEditor}>
                                    <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 14 14" fill="none">
                                        <path d="M14 2.16901L11.831 0L7 4.92958L2.16901 0L0 2.16901L4.92958 7L0 11.831L2.16901 14L7 9.07042L11.831 14L14 11.831L9.07042 7L14 2.16901Z" fill="white" />
                                    </svg>
                                </button>
                            </div>
                            <span className={styles.title}>{editorData.title}</span>
                            <textarea ref={textareaRef} className={styles.textarea}></textarea>
                            <div className={styles.buttons_container}>
                                <button className={classNames(
                                    _styles.invite_code_cancel_button,
                                    _styles.filter_button
                                )} onClick={closeEditor}>Отмена</button>
                                <button className={_styles.create_invite_code_button} onClick={saveData}>Сохранить</button>
                            </div>
                        </div>
                    </div>
                </>
            }
            {children}
        </EditorContext.Provider>
    );
}

export { EditorProvider, EditorContext };