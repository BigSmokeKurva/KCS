import { useLocation } from "react-router-dom";
import { useState, useRef, useEffect, useCallback } from "react";
import SwitchComponent from "./components/Switch";
import InviteCodesMenu from "./components/InviteCodesMenu";
import InviteCodeBody from "./components/InviteCodeBody";
import UsersMenu from "./components/UsersMenu";
import UserBody from "./components/UserBody";
import styles from "./style.module.css";
import { EditorProvider } from "./contexts/EditorContext";
import DropdownContainer from "../shared_components/DropdownContainer";


function AdminPanel({isMobile}) {
  const location = useLocation();
  const page = location.pathname.replace("/", "");
  const [selectedCode, setSelectedCode] = useState({});
  const [selectedUser, setSelectedUser] = useState({});
  const inviteCodesRef = useRef(null);
  const usersRef = useRef(null);

  function addCode(code) {
    inviteCodesRef.current.addAndSelectCode(code);
    setSelectedCode(code);
  }

  function deleteCode(code) {
    inviteCodesRef.current.deleteCode(code);
  }

  function editUsernameInList(id, newUsername) {
    usersRef.current.editUsernameInList(id, newUsername);
  }

  function editAdminInList(id, admin) {
    usersRef.current.editAdminInList(id, admin);
  }

  function deleteUser(id) {
    setSelectedUser({});
    usersRef.current.deleteFromList(id);
  }

  return (
    <EditorProvider>
      <div className={styles.main_container}>
        {
          isMobile ?
            (
              page === "admin-panel" ?
                <DropdownContainer title="Сотрудники">
                  <UsersMenu isMobile={isMobile} selectedUser={selectedUser} setSelectedUser={setSelectedUser} ref={usersRef} />
                </DropdownContainer> :
                <DropdownContainer title="Инвайт-коды">
                  <InviteCodesMenu isMobile={isMobile} selectedCode={selectedCode} setSelectedCode={setSelectedCode} ref={inviteCodesRef} />
                </DropdownContainer>
            ) :
            (
              page === "admin-panel" ?
                <UsersMenu selectedUser={selectedUser} setSelectedUser={setSelectedUser} ref={usersRef} /> :
                <InviteCodesMenu selectedCode={selectedCode} setSelectedCode={setSelectedCode} ref={inviteCodesRef} />
            )
        }
        <div className={styles.page_content}>
          <SwitchComponent _selectedMode={page} />
          {
            page === "admin-panel" ?
              (page === "admin-panel" && selectedUser.id) && <UserBody isMobile={isMobile} editUsernameInList={editUsernameInList} editAdminInList={editAdminInList} _deleteUser={deleteUser} userShort={selectedUser} /> :
              (page === "invite-codes" && selectedCode.code) && <InviteCodeBody code={selectedCode} setSelectedCode={setSelectedCode} addCode={addCode} removeCode={deleteCode} />
          }
        </div>
      </div>
    </EditorProvider>
  );
}

export default AdminPanel;
