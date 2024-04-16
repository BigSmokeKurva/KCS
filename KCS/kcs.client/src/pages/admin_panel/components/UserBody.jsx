import { useState, useEffect, useCallback, useContext, useRef } from "react";
import Cookies from "js-cookie";
import styles from "../style.module.css";
import UserEditField from "./UserEditField";
import TokensDropdown from "./TokensDropdown";
import ManagmentDropdown from "./ManagmentDropdown";
import Logs from "./Logs";
import DropdownContainer from "../../shared_components/DropdownContainer";
import { NotificationsContext } from "../../../contexts/notification/NotificationsContext";
import { FileReaderContext } from "../../../contexts/fileReader/FileReaderContext";
import { FileDownloadContext } from "../../../contexts/fileDownload/FileDownloadContext";
import { EditorContext } from "../contexts/EditorContext";
import classNames from "classnames";
import Checkbox from "./Checkbox";

function UserBody({ userShort, editUsernameInList, editAdminInList, _deleteUser, isMobile }) {
  // username = 0, password = 1, admin = 2, tokens = 3, paused = 4
  const [user, setUser] = useState({});
  const { showNotification } = useContext(NotificationsContext);
  const { readFile } = useContext(FileReaderContext);
  const { downloadFile } = useContext(FileDownloadContext);
  const { openEditor } = useContext(EditorContext);
  const usernameFieldRef = useRef(null);
  const passwordFieldRef = useRef(null);

  useEffect(() => {
    getUser();
  }, [userShort]);

  useEffect(() => {
    usernameFieldRef.current.setInputValue(user.username || "");
    passwordFieldRef.current.setInputValue(user.password || "");
  }, [user]);

  const setUsername = useCallback(async () => {
    const newUsername = usernameFieldRef.current.getInputValue();
    if (newUsername === user.username) {
      return;
    }
    var auth_token = Cookies.get("auth_token");
    var data = {
      id: userShort.id,
      value: newUsername,
      property: 0
    };
    const response = await fetch("/api/admin/edituser", {
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
      usernameFieldRef.current.setInputValue(user.username);
    } else {
      setUser({ ...user, username: newUsername });
      editUsernameInList(userShort.id, newUsername);
    }
  });

  const setPassword = useCallback(async () => {
    const newPassword = passwordFieldRef.current.getInputValue();
    if (newPassword === user.password) {
      return;
    }
    var auth_token = Cookies.get("auth_token");
    var data = {
      id: userShort.id,
      value: newPassword,
      property: 1
    };
    const response = await fetch("/api/admin/edituser", {
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
      passwordFieldRef.current.setInputValue(user.password);
    } else {
      setUser({ ...user, password: newPassword });
    }
  });

  const getUser = useCallback(async () => {
    var auth_token = Cookies.get("auth_token");
    const response = await fetch("/api/admin/getuserinfo?id=" + userShort.id, {
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
    setUser(data);
  });

  const uploadTokens = useCallback(async () => {
    var result = await new Promise(resolve => readFile(resolve));
    showNotification("Проверка токенов...", "warning");
    const lines = result.split('\n')
      .map(line => line.trim().replace(/\r/g, ''))
      .filter(line => line !== '');
    var auth_token = Cookies.get("auth_token");
    var data = {
      id: userShort.id,
      value: lines,
      property: 3
    };
    const response = await fetch("/api/admin/edituser", {
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
    result = await response.json();

    if (result.status !== "ok") {
      showNotification(result.message, "error");
      return;
    }
    showNotification("Токены загружены", "success");
    setUser({ ...user, tokensCount: result.message });
  });

  const downloadTokens = useCallback(async () => {
    var auth_token = Cookies.get("auth_token");
    const response = await fetch("/api/admin/gettokens?id=" + userShort.id + "&usernames=false", {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
        "Authorization": auth_token,
      }
    });
      if (response.redirected) {
          window.location.href = response.url;
      }
    const result = await response.json();
    downloadFile("tokens.txt", result.join('\n'));
  });

  const openEditorTokens = useCallback(async () => {
    const auth_token = Cookies.get("auth_token");
    const response = await fetch("/api/admin/gettokens?id=" + userShort.id + "&usernames=true", {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
        "Authorization": auth_token,
      }
    });
      if (response.redirected) {
          window.location.href = response.url;
      }
    const result = await response.json();

    openEditor({
      _editorData: {
        title: "Токены",
        text: result.join('\n'),
        callback: saveEditorTokens
      }
    });
  });

  const saveEditorTokens = useCallback(async (text) => {
    showNotification("Проверка токенов...", "warning");
    var auth_token = Cookies.get("auth_token");
    var data = {
      id: userShort.id,
      value: text.split('\n'),
      property: 3
    };
    const response = await fetch("/api/admin/edituser", {
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
    showNotification("Токены загружены", "success");

    setUser({ ...user, tokensCount: result.message });
  });

  const editAdmin = useCallback(async () => {
    const auth_token = Cookies.get("auth_token");
    var data = {
      id: userShort.id,
      value: !user.admin,
      property: 2
    };
    const response = await fetch("/api/admin/edituser", {
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
    setUser({ ...user, admin: !user.admin });
    editAdminInList(userShort.id, !user.admin);
  });

  const editPaused = useCallback(async () => {
    const auth_token = Cookies.get("auth_token");
    var data = {
      id: userShort.id,
      value: !user.paused,
      property: 4
    };
    const response = await fetch("/api/admin/edituser", {
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
    setUser({ ...user, paused: !user.paused });
  });

  const deleteUser = useCallback(async () => {
    const auth_token = Cookies.get("auth_token");
    const response = await fetch("/api/admin/deleteuser?id=" + userShort.id, {
      method: "DELETE",
      headers: {
        "Content-Type": "application/json",
        "Authorization": auth_token,
      },
    });
      if (response.redirected) {
          window.location.href = response.url;
      }
    const result = await response.json();

    if (result.status !== "ok") {
      showNotification("Произошла неизвестная ошибка", "error");
      return;
    }
    _deleteUser(userShort.id);
    showNotification("Пользователь удален", "success");
  });


  const user_info = (
    <div className={styles.user_info}>
      <div className={styles.user_info_item}>
        <span>Логин</span>
        <UserEditField ref={usernameFieldRef} callbackFunc={setUsername} placeholder="Введите логин" />
      </div>
      <div className={styles.user_info_item}>
        <span>Пароль</span>
        <UserEditField ref={passwordFieldRef} callbackFunc={setPassword} placeholder="Введите пароль" />
      </div>
      <div className={styles.user_info_item}>
        <span>Инвайт-код</span>
        <div className={styles.user_info_invite_code}>{user.token}{user.inviteCode || "Удален"}</div>
      </div>
      {
        isMobile ? (
          <>
            <div className={classNames(
              styles.user_info_item,
              styles.tokens_info
            )}>
              <span>Токены</span>
              <div className={styles.tokens_info_value}>
                <span>{user.tokensCount}</span>
                строк
              </div>
              <button onClick={openEditorTokens} className={styles.edit_tokens_button}>Редактор токенов</button>
              <button onClick={uploadTokens}>Загрузить токены</button>
              <button onClick={downloadTokens}>Выгрузить токены</button>
            </div>
            <div className={classNames(
              styles.user_info_item,
              styles.tokens_info,
              styles.managment_info
            )}>
              <span>Управление</span>
              <div>
                Пауза
                <Checkbox checked={user.paused} onClick={editPaused} />
              </div>
              <div>
                Права админа
                <Checkbox checked={user.admin} onClick={editAdmin} />
              </div>
              <button onClick={deleteUser}>Удалить аккаунт</button>
            </div>
          </>
        ) : (
          <>
            <div className={classNames(
              styles.user_info_item,
              styles.tokens_info
            )}>
              <span>Токены</span>
              <TokensDropdown
                items={[
                  { title: "Загрузить", callback: uploadTokens },
                  { title: "Выгрузить", callback: downloadTokens },
                  { title: "Редактировать", callback: openEditorTokens }
                ]}
              />
              <div className={styles.tokens_info_value}>
                <span>{user.tokensCount}</span>
                строк
              </div>
            </div>
            <div className={classNames(
              styles.user_info_item,
              styles.tokens_info
            )}>
              <span>Управление</span>
              <ManagmentDropdown paused={user.paused} editPaused={editPaused} admin={user.admin} editAdmin={editAdmin} deleteUser={deleteUser} />
            </div>
          </>
        )
      }
    </div>
  )

  return (
    <div className={styles.user_body}>
      {
        isMobile ? (
          <DropdownContainer title="Управление">
            {user_info}
          </DropdownContainer>
        ) : user_info
      }
      <Logs user={user} />
    </div>
  );
}

export default UserBody;