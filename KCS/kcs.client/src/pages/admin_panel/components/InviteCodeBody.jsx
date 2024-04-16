import styles from "../style.module.css";
import classNames from "classnames";
import { useState, useContext, useCallback, useEffect } from "react";
import Cookies from "js-cookie";
import InviteCodeTimeDropdown from "./InviteCodeTimeDropdown";
import { NotificationsContext } from "../../../contexts/notification/NotificationsContext";

function InviteCodeBody({ code, setSelectedCode, addCode, removeCode }) {
    const [selectedTime, setSelectedTime] = useState(12);
    const [selectedMode, setSelectedMode] = useState(code.mode);
    const { showNotification } = useContext(NotificationsContext);
    const disabledTime = code.status !== "NotCreated";
    const [activationDate, activationTime] = formatDateTime(code.activationdate);

    function formatDate(dateString) {
        const date = new Date(dateString);
        return date.toLocaleString('ru', { day: '2-digit', month: '2-digit', year: '2-digit', hour: '2-digit', minute: '2-digit' }).replace(/-/g, '.').replace(/,/, ' ');
    }

    function formatDateTime(dateString) {
        const date = new Date(dateString);
        const dateFormatted = date.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric' }).replace(/\./g, '.');
        const timeFormatted = date.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
        return [dateFormatted, timeFormatted];
    }

    useEffect(() => {
        setSelectedMode(code.mode);
        setSelectedTime(12);
    }, [code]);

    const copyCode = useCallback(async () => {
        try {
            await navigator.clipboard.writeText(code.code);
            showNotification("Инвайт код скопирован", "success");
        } catch (error) {
            showNotification("Ошибка копирования", "error");
        }
    });

    const createCode = useCallback(async () => {
        var auth_token = Cookies.get("auth_token");
        var data = {
            code: code.code,
            mode: selectedMode,
            hours: selectedTime
        };

        const response = await fetch("/api/admin/createinvitecode", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Authorization": auth_token,
            },
            body: JSON.stringify(data)
        });
        if (response.redirected) {
            window.location.href = response.url;
        }
        const result = await response.json();

        if (result.status !== "ok") {
            showNotification(result.message, "error");
            return;
        }

        showNotification("Инвайт код создан", "success");

        code = result.code;
        addCode(code);
        setSelectedCode(code);
    });

    const deleteCode = useCallback(async () => {
        var auth_token = Cookies.get("auth_token");

        const response = await fetch("/api/admin/deleteinvitecode?code=" + code.code, {
            method: "DELETE",
            headers: {
                "Content-Type": "application/json",
                "Authorization": auth_token,
            }
        });
        if (response.redirected) {
            window.location.href = response.url;
        }
        const result = await response.json();

        if (result.status !== "ok") {
            showNotification(result.message, "error");
            return;
        }

        showNotification("Инвайт код удален", "success");
        setSelectedCode({});
        removeCode(code);
    });

    return (
        <div className={styles.invite_code_content}>
            <div className={styles.first_column}>
                <div className={styles.invite_code_title}>
                    Инвайт код
                    <span className={classNames(
                        styles.invite_code_status,
                        code.status === "Active" && styles.active,
                        code.status === "Used" && styles.used,
                        code.status === "Expired" && styles.expired,
                        code.status === "NotCreated" && styles.notcreated

                    )}>
                        {code.status === "Active" ? "НЕ АКТИВИРОВАН" :
                            code.status === "Used" ? "АКТИВИРОВАН" :
                                code.status === "Expired" ? "ИСТЕК" : "НЕ СОЗДАН"}
                    </span>
                </div>
                <div className={styles.label_copy} onClick={copyCode}>
                    {code.code}
                    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 20 20" fill="none">
                        <path d="M5.83334 8.05581C5.83334 7.46637 6.0675 6.90107 6.4843 6.48427C6.9011 6.06747 7.4664 5.83331 8.05584 5.83331H15.2775C15.5694 5.83331 15.8584 5.8908 16.128 6.00249C16.3977 6.11418 16.6427 6.27789 16.8491 6.48427C17.0554 6.69065 17.2191 6.93565 17.3308 7.2053C17.4425 7.47495 17.5 7.76395 17.5 8.05581V15.2775C17.5 15.5693 17.4425 15.8583 17.3308 16.128C17.2191 16.3976 17.0554 16.6426 16.8491 16.849C16.6427 17.0554 16.3977 17.2191 16.128 17.3308C15.8584 17.4425 15.5694 17.5 15.2775 17.5H8.05584C7.76398 17.5 7.47498 17.4425 7.20533 17.3308C6.93568 17.2191 6.69068 17.0554 6.4843 16.849C6.27792 16.6426 6.11421 16.3976 6.00252 16.128C5.89083 15.8583 5.83334 15.5693 5.83334 15.2775V8.05581Z" stroke="#9147FF" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
                        <path d="M3.34333 13.9475C3.08779 13.8018 2.87523 13.5912 2.72715 13.3371C2.57906 13.0829 2.50071 12.7942 2.5 12.5V4.16667C2.5 3.25 3.25 2.5 4.16667 2.5H12.5C13.125 2.5 13.465 2.82083 13.75 3.33333" stroke="#9147FF" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
                    </svg>
                </div>
                <div className={styles.invite_code_mode} disabled={disabledTime}>
                    <span disabled={disabledTime}>Режим активации</span>
                    <div disabled={disabledTime}>
                        <button disabled={disabledTime} className={classNames(
                            styles.invite_code_mode_button,
                            selectedMode === "Time" && styles.selected
                        )} onClick={() => setSelectedMode("Time")}></button>
                        <span disabled={disabledTime}>Лимит по времени</span>
                    </div>
                    <div disabled={disabledTime}>
                        <button disabled={disabledTime} className={classNames(
                            styles.invite_code_mode_button,
                            selectedMode === "Unlimited" && styles.selected
                        )} onClick={() => setSelectedMode("Unlimited")}></button>
                        <span disabled={disabledTime}>Бессрочный</span>
                    </div>
                    <InviteCodeTimeDropdown
                        disabled={disabledTime || selectedMode === "Unlimited"}
                        selectedTime={selectedTime}
                        setSelectedTime={setSelectedTime}
                        items={[2, 4, 12, 24, 48]}
                    />
                </div>
                {
                    (code.status === "Active" && selectedMode === "Time") &&
                    <div className={styles.expires_data}>
                        Активен до
                        <span>{formatDate(code.expires)}</span>
                    </div>
                }
            </div>
            <div className={styles.second_column}>
                <div className={styles.invite_code_info_row}>
                    Логин
                    <span>{code.username ? code.username : "-"}</span>
                </div>
                <div className={styles.invite_code_info_row}>
                    Дата активации
                    <span>{code.username ? activationDate : "-"}</span>
                </div>
                <div className={styles.invite_code_info_row}>
                    Время активации
                    <span>{code.username ? activationTime : "-"}</span>
                </div>
                <div className={styles.invite_code_content_buttons}>
                    <button className={styles.invite_code_cancel_button} onClick={code.status === "NotCreated" ? () => { setSelectedCode({}) } : deleteCode}>
                        {code.status === "NotCreated" ? "Отменить" : "Удалить"}
                    </button>
                    {
                        code.status === "NotCreated" &&
                        <button className={styles.create_invite_code_button} onClick={createCode}>
                            Создать
                        </button>
                    }
                </div>
            </div>
        </div>
    )
}

export default InviteCodeBody;