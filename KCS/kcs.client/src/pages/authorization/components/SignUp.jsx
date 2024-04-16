import { useRef, useState, useContext } from 'react';
import { useNavigate } from 'react-router-dom';
import FieldInput from "./FieldInput";
import { NotificationsContext } from '../../../contexts/notification/NotificationsContext';
import styles from '../style.module.css';

function SignUp() {
  const loginRef = useRef(null);
  const passwordRef = useRef(null);
  const inviteCodeRef = useRef(null);
  const registerButtonRef = useRef(null);
  const [sendButtonActive, setSendButtonActive] = useState(false);
  const {showNotification} = useContext(NotificationsContext);
  const navigate = useNavigate();

  async function signUp(){
    const login = loginRef.current.getInputValue();
    const password = passwordRef.current.getInputValue();
    const inviteCode = inviteCodeRef.current.getInputValue();

    var body = {
      login: login,
      password: password,
      inviteCode: inviteCode
    };

    var response = await fetch('/api/auth/signup', {
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

    if(result.status !== "ok"){
      result.errors.forEach(error => {
        switch(error.type){
          case "login":
            loginRef.current.setError(error.message);
            break;
          case "password":
            passwordRef.current.setError(error.message);
            break;
          case "inviteCode":
            inviteCodeRef.current.setError(error.message);
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

  function checkAllFields(event, ref){
    // Enter
    if(event.keyCode === 13){
      switch(ref){
        case loginRef:
          passwordRef.current.focus();
          break;
        case passwordRef:
          inviteCodeRef.current.focus();
          break;
        case inviteCodeRef:
          if(sendButtonActive){
            registerButtonRef.current.click();
          }
          break;
      }
      return;
    }

    const login = loginRef.current.getInputValue();
    const password = passwordRef.current.getInputValue();
    const inviteCode = inviteCodeRef.current.getInputValue();

    const loginValid = login.length >= 4 && login.length <= 16 ;
    const passwordValid = password.length >= 6 && password.length <= 30;
    const inviteCodeValid = inviteCode.length >= 5 && inviteCode.length <= 32;
    
    const valid = loginValid && passwordValid && inviteCodeValid;

    if(!sendButtonActive && valid){
      setSendButtonActive(true);
    }
    else if(sendButtonActive && !valid){
      setSendButtonActive(false);
    }
  }

  return (
    <div className={styles.content_body}>
      <FieldInput ref={loginRef} text="Логин" type="text" onKeyUp={checkAllFields}/>
      <FieldInput ref={passwordRef} text="Пароль" type="password" onKeyUp={checkAllFields}/>
      <FieldInput ref={inviteCodeRef} text="Инвайт код" type="text" onKeyUp={checkAllFields}/>
      <button ref={registerButtonRef} onClick={signUp} disabled={!sendButtonActive}>Зарегистрироваться</button>
    </div>
  )
}
export default SignUp;