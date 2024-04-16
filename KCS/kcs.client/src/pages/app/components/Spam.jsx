import styles from "../style.module.css";
import _styles from "../../../pages/admin_panel/style.module.css";
import { useState, useCallback, useEffect, useContext } from "react";
import classNames from "classnames";
import TemplatesDropdown from "./TemplatesDropdown";
import SpamInputField from "./SpamInputField";
import Cookies from "js-cookie";
import { NotificationsContext } from "../../../contexts/notification/NotificationsContext";
import { SpamEditorContext } from "../contexts/SpamEditorContext";

function Spam() {
    const [selectedMode, setSelectedMode] = useState("random");
    const [selectedTemplate, setSelectedTemplate] = useState({});
    const [disabled, setDisabled] = useState(true);
    const [templates, setTemplates] = useState([]);
    const [delayValue, setDelayValue] = useState("0");
    const [threadsValue, setThreadsValue] = useState("0");
    const { showNotification } = useContext(NotificationsContext);
    const [isStarted, setIsStarted] = useState(false);
    const {openEditor} = useContext(SpamEditorContext);

    const getSpamTemplates = useCallback(async () => {
        const auth_token = Cookies.get("auth_token");
        const response = await fetch("/api/app/getspamtemplates", {
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
        setTemplates(data);
        if(data.length > 0){
            setSelectedTemplate(data[0]);
        }
    });

    const getIsStarted = useCallback(async () => {
        const auth_token = Cookies.get("auth_token");
        const response = await fetch("/api/app/spamisstarted", {
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
        setIsStarted(data.isStarted);
    });

    useEffect(() => {
        getSpamTemplates();
        getIsStarted();
    }, []);

    useEffect(() => {
        if(isStarted){
            const interval = setInterval(getIsStarted, 5000);
            return () => clearInterval(interval);
        }
    }, [isStarted]);

    useEffect(() =>{
        if(!selectedTemplate.title) return;
        setDisabled(false);
        setDelayValue(selectedTemplate.delay);
        setThreadsValue(selectedTemplate.threads);
        setSelectedMode(selectedTemplate.mode);
    }, [selectedTemplate]);

    const updateTemplate = useCallback(async () => {
        const auth_token = Cookies.get("auth_token");
        const response = await fetch("/api/app/updatespamconfiguration", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Authorization": auth_token,
            },
            body: JSON.stringify({
                title: selectedTemplate.title,
                oldTitle: selectedTemplate.title,
                mode: selectedMode === "random" ? 0 : 1,
                delay: delayValue,
                threads: threadsValue,
                messages: selectedTemplate.messages
            })
        });
        if (response.redirected) {
            window.location.href = response.url;
        }
        const data = await response.json();
        if(data.status !== "ok"){
            showNotification(data.message, "error");
            return false;
        }
        return true;
    }, [selectedTemplate, selectedMode, delayValue, threadsValue]);

    const startSpam = useCallback(async () => {
        if(!await updateTemplate()) return;
        const auth_token = Cookies.get("auth_token");
        const response = await fetch("/api/app/startspam?title=" + selectedTemplate.title, {
            method: "GET",
            headers: {
                "Content-Type": "application/json",
                "Authorization": auth_token,
            },
        });
        if (response.redirected) {
            window.location.href = response.url;
        }
        const data = await response.json();
        if(data.status !== "ok"){
            showNotification(data.message, "error");
            return;
        }
        setIsStarted(true);

    });

    const stopSpam = useCallback(async () => {
        const auth_token = Cookies.get("auth_token");
        const response = await fetch("/api/app/stopspam", {
            method: "GET",
            headers: {
                "Content-Type": "application/json",
                "Authorization": auth_token,
            },
        });
        if (response.redirected) {
            window.location.href = response.url;
        }
        const data = await response.json();
        if(data.status !== "ok"){
            showNotification(data.message, "error");
            return;
        }
        setIsStarted(false);
    });

    return (
        <div className={styles.spam_container}>
            <div className={styles.spam_row}>
                <div>
                    <span className={styles.title}>Режим работы</span>
                    <div className={classNames(
                        _styles.invite_code_mode,
                        styles.spam_mode
                    )} disabled={disabled}>
                        <div disabled={disabled}>
                            <button disabled={disabled} className={classNames(
                                _styles.invite_code_mode_button,
                                selectedMode === "random" && _styles.selected
                            )} onClick={() => setSelectedMode("random")}></button>
                            <span disabled={disabled}>Случайный</span>
                        </div>
                        <div disabled={disabled}>
                            <button disabled={disabled} className={classNames(
                                _styles.invite_code_mode_button,
                                selectedMode === "list" && _styles.selected
                            )} onClick={() => setSelectedMode("list")}></button>
                            <span disabled={disabled}>По списку</span>
                        </div>
                    </div>
                </div>
                <div>
                    <span className={styles.title}>Шаблоны</span>
                    <TemplatesDropdown templates={templates} selectedTemplate={selectedTemplate} setSelectedTemplate={setSelectedTemplate} />
                </div>
            </div>
            <div className={styles.spam_row}>
                <span className={styles.title}>Настройки спама</span>
                <div className={styles.spam_inputs}>
                    <SpamInputField disabled={disabled} title="задержка" rightText="сек" value={delayValue} setValue={setDelayValue} />
                    <SpamInputField disabled={disabled} title={selectedMode === "random" ? "потоки" : "боты"} value={threadsValue} setValue={setThreadsValue} />
                </div>
            </div>
            <div className={styles.spam_row}>
                <button className={classNames(
                    _styles.create_invite_code_button,
                    styles.spam_open_editor_button
                )} disabled={isStarted} onClick={() => openEditor({_callback:getSpamTemplates})}>Редактировать</button>
                <button className={_styles.create_invite_code_button} disabled={disabled} onClick={isStarted ? stopSpam : startSpam}>
                    {
                        isStarted ? "Остановить" : "Запустить"
                    }
                </button>
            </div>
        </div>
    )
}

export default Spam;