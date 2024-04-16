import FieldInput from "./FieldInput";
import { useRef, useState, useContext } from 'react';
import { useNavigate } from "react-router-dom";
import { NotificationsContext } from '../../../contexts/notification/NotificationsContext';
import styles from '../style.module.css';

function SignIn() {
  const loginRef = useRef(null);
  const passwordRef = useRef(null);
  const loginButtonRef = useRef(null);
  const [sendButtonActive, setSendButtonActive] = useState(false);
  const { showNotification } = useContext(NotificationsContext);
  const navigate = useNavigate();

  async function signIn() {
    const login = loginRef.current.getInputValue();
    const password = passwordRef.current.getInputValue();

    var body = {
      login: login,
      password: password,
    };

    var response = await fetch('/api/auth/signin', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(body)
    });
      if (response.redirected) {
          window.location.href = response.url;
      }
    var result = await response.json();

    if (result.status !== "ok") {
      result.errors.forEach(error => {
        switch (error.type) {
          case "login":
            loginRef.current.setError(error.message);
            break;
          case "password":
            passwordRef.current.setError(error.message);
            break;
          case "notification":
            showNotification(error.message, "error");
            break;
        }
      });
      setSendButtonActive(false);
      return;
    }
    navigate('/app');
  }

  function checkAllFields(event, ref) {
    // Enter
    if (event.keyCode === 13) {
      switch (ref) {
        case loginRef:
          passwordRef.current.focus();
          break;
        case passwordRef:
          if (sendButtonActive) {
            loginButtonRef.current.click();
          }
          break;
      }
      return;
    }

    const login = loginRef.current.getInputValue();
    const password = passwordRef.current.getInputValue();

    const loginValid = login.length >= 4 && login.length <= 16;
    const passwordValid = password.length >= 6 && password.length <= 30;

    const valid = loginValid && passwordValid;

    if (!sendButtonActive && valid) {
      setSendButtonActive(true);
    }
    else if (sendButtonActive && !valid) {
      setSendButtonActive(false);
    }
  }

  return (
    <div className={styles.content_body}>
      <FieldInput ref={loginRef} text="Логин" type="text" onKeyUp={checkAllFields} />
      <FieldInput ref={passwordRef} text="Пароль" type="password" onKeyUp={checkAllFields} />
      <button ref={loginButtonRef} onClick={signIn} disabled={!sendButtonActive}>Войти</button>
    </div>
  )
}
export default SignIn;