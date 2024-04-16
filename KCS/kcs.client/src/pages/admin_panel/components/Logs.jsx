import styles from "../style.module.css";
import { useState, useEffect, useCallback, useRef } from "react";
import LogsTimeDropdown from "./LogsTimeDropdown";
import LogsSwitch from "./LogsSwitch";
import Cookies from "js-cookie";
import classNames from "classnames";

function Logs({ user }) {
    const [selectedDate, setSelectedDate] = useState(null);

    const [selectedMode, setSelectedMode] = useState("chat");
    const [logs, setLogs] = useState([]);
    const [currentUser, setCurrentUser] = useState(null);
    useEffect(() => {
        if (!user.logsTime) {
            return;
        }
        if (!currentUser || currentUser.id !== user.id) {
            setCurrentUser(user);
            setSelectedDate(user.logsTime[0]);
        }
    }, [user])

    const getLogs = useCallback(async () => {
        const auth_token = Cookies.get("auth_token");
        const response = await fetch(`/api/admin/getlogs?id=${user.id}&time=${selectedDate}&type=${selectedMode === "chat" ? 0 : 1}`, {
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

        setLogs(data);
    });

    function formatTime(dateString) {
        const date = new Date(dateString);
        const timeFormatted = date.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
        return timeFormatted;
    }

    useEffect(() => {
        if (!user.logsTime) {
            return;
        }
        getLogs();
    }, [selectedDate, selectedMode, currentUser]);

    return (
        <div className={styles.logs_container}>
            <div className={styles.logs_buttons_container}>
                <LogsTimeDropdown selectedTime={selectedDate} setSelectedTime={setSelectedDate} items={user.logsTime} />
                <LogsSwitch selectedMode={selectedMode} setSelectedMode={setSelectedMode} />
            </div>
            <div className={styles.logs_table_header}>
                <span>Время</span>
                <span>Действие</span>
            </div>
            {
                logs.length !== 0 &&
                <div className={classNames(
                    styles.list_container,
                    styles.logs_list_container
                )}>
                    <div className={styles.logs_table}>
                        {
                            logs.map((log, index) => (
                                <div key={index} className={classNames(
                                    styles.logs_table_row,
                                    (index + 1) % 2 === 0 ? styles.even : styles.odd
                                )}>
                                    <span>{formatTime(log.time)}</span>
                                    <span>{log.message}</span>
                                </div>
                            ))
                        }
                    </div>
                </div>
            }
        </div>
    )
}

export default Logs;