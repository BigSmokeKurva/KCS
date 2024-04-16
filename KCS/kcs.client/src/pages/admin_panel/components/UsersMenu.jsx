import { useState, useCallback, useEffect, forwardRef, useImperativeHandle, useContext } from "react";
import Cookies from "js-cookie";
import styles from "../style.module.css";
import classNames from "classnames";
import { EditorContext } from "../contexts/EditorContext";
import { NotificationsContext } from "../../../contexts/notification/NotificationsContext";

const UsersMenu = forwardRef(({ selectedUser, setSelectedUser, isMobile }, ref) => {
    const [usersList, setUsersList] = useState([]);
    const [searchText, setSearchText] = useState("");
    const { openEditor } = useContext(EditorContext);
    const { showNotification } = useContext(NotificationsContext);

    useEffect(() => {
        getUsers();
    }, [])

    useImperativeHandle(ref, () => ({
        editUsernameInList: (id, newUsername) => {
            setUsersList(usersList.map(x => x.id === id ? { ...x, username: newUsername } : x));
        },
        deleteFromList: (id) => {
            setUsersList(usersList.filter(x => x.id !== id));
        },
        editAdminInList: (id, admin) => {
            setUsersList(usersList.map(x => x.id === id ? { ...x, admin } : x));
        }
    }));

    const getUsers = useCallback(async () => {
        var auth_token = Cookies.get("auth_token");
        const response = await fetch("/api/admin/getusers", {
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
        setUsersList(data);
    });

    function selectUser(user) {
        if (selectedUser.id === user.id) {
            return;
        }
        setSelectedUser(user);
    }

    const openEditorFilter = useCallback(async () => {
        const auth_token = Cookies.get("auth_token");
        const response = await fetch("/api/admin/getfilter", {
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

        openEditor({
            _editorData: {
                title: "Фильтр слов",
                text: data.join("\n"),
                callback: saveEditorFilter
            }
        });
    });

    const saveEditorFilter = useCallback(async (text) => {
        const auth_token = Cookies.get("auth_token");
        const response = await fetch("/api/admin/uploadfilter", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Authorization": auth_token,
            },
            body: JSON.stringify(text.split("\n"))
        });
        if (response.redirected) {
            window.location.href = response.url;
        }
        const result = await response.json();

        if (result.status === "ok") {
            showNotification("Фильтр слов сохранен", "success");
        } else {
            showNotification("Неизвестная ошибка", "error");
        }
    });

    return (
        <div className={styles.invite_menu_container}>
            {
                !isMobile &&
                <div className={styles.title}>
                    Сотрудники
                </div>
            }
            <div className={styles.search_container}>
                <input className={styles.search_input} type="text" placeholder="Поиск" onKeyUp={(event) => setSearchText(event.target.value.toLowerCase())} />
                <button className={classNames(
                    styles.invite_code_cancel_button,
                    styles.filter_button
                )} onClick={openEditorFilter}>Фильтр слов</button>
            </div>
            <div className={styles.list_container}>
                <div className={styles.invite_list}>
                    {
                        usersList.filter(x => x.username.toLowerCase().includes(searchText)).map((item) => {
                            return (
                                <button className={classNames(
                                    styles.invite_code_list_item,
                                    selectedUser.id === item.id && styles.selected
                                )} key={item.id} onClick={() => selectUser(item)}>
                                    <div>{item.username}</div>
                                    {
                                        item.admin &&
                                        <span className={styles.admin_ico}>
                                            ADM
                                        </span>
                                    }
                                </button>
                            )
                        })
                    }
                </div>
            </div>
        </div>
    )
});

export default UsersMenu;