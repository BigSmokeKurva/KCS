import React from 'react';
import styles from "./style.module.css";
import BotList from './components/BotList';
import Stream from './components/Stream';
import Binds from './components/Binds';
import Spam from './components/Spam';
import BindsDropdownContainer from './components/BindsDropdownContainer';
import EditStreamerUsername from './components/EditStreamerUsername';
import DropdownContainer from '../shared_components/DropdownContainer';
import { FollowBotProvider } from './contexts/FollowBotContext';
import { BindsEditorProvider } from './contexts/BindsEditorContext';
import { SpamEditorProvider } from './contexts/SpamEditorContext';
import Chat from './components/Chat';
import { useState, useEffect, useCallback, useRef, useContext } from 'react';
import Cookies from 'js-cookie';
import { NotificationsContext } from '../../contexts/notification/NotificationsContext';

function App({ headerRef, isMobile }) {
  const [user, setUser] = useState(null);
  const editStreamerUsernameRef = useRef(null);
  const { showNotification } = useContext(NotificationsContext);
  const botListRef = useRef(null);
  const [streamOnline, setStreamOnline] = useState(false);
  const [binds, setBinds] = useState([]);
  const [keys, setKeys] = useState([]);
  const [isSent, setIsSent] = useState(false);

  const getUser = useCallback(async () => {
    var auth_token = Cookies.get('auth_token');
    var response = await fetch('/api/app/getuser', {
      method: 'GET',
      headers: {
        'Content-Type': "application/json",
        'Authorization': auth_token
      }
    });
    if (response.redirected) {
      window.location.href = response.url;
    }
    var result = await response.json();
    setUser(result);
  });

  const editStreamerUsername = useCallback(async () => {
    const streamerUsername = editStreamerUsernameRef.current.getInputValue();
    if (streamerUsername === user.streamerUsername) return;
    var auth_token = Cookies.get('auth_token');
    var response = await fetch('/api/app/updateStreamerUsername?username=' + streamerUsername, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': auth_token
      },
    });

      if (response.redirected) {
          window.location.href = response.url;
      }

    var result = await response.json();

    if (result.status !== "ok") {
      showNotification(result.message, "error");
      editStreamerUsernameRef.current.setInputValue(user.streamerUsername);
      return;
    }
    showNotification("Ник стримера успешно обновлен", "success");

    setUser({ ...user, streamerUsername });
    botListRef.current.setBots(botListRef.current.getBots().map(bot => ({
      ...bot,
      isConnected: false,
      isQueue: false,
    })));

  });

  const getViewerCount = useCallback(async () => {
    const data = [
      {
        "operationName": "UseViewCount",
        "variables": {
          "channelLogin": user.streamerUsername
        },
        "extensions": {
          "persistedQuery": {
            "version": 1,
            "sha256Hash": "00b11c9c428f79ae228f30080a06ffd8226a1f068d6f52fbc057cbde66e994c2"
          }
        }
      }
    ]
    var viewersCount = 0;
    const response = await fetch('https://gql.twitch.tv/gql', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        "Client-Id": "kimne78kx3ncx6brgo4mv6wki5h1ko"
      },
      body: JSON.stringify(data)
    });
      if (response.redirected) {
          window.location.href = response.url;
      }
    const result = await response.json();
    if (result[0].data.user === null) {
      viewersCount = "-";
      setStreamOnline(false);
    }
    else if (result[0].data.user.stream === null) {
      viewersCount = "-";
      setStreamOnline(false);
    }
    else {
      viewersCount = result[0].data.user.stream.viewersCount;
      setStreamOnline(true);
    }
    headerRef.current.setViewersCount(viewersCount);
  });

  const getBinds = useCallback(async () => {
    var auth_token = Cookies.get('auth_token');
    var response = await fetch('/api/app/getbinds', {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': auth_token
      }
    });
      if (response.redirected) {
          window.location.href = response.url;
      }
    var result = await response.json();
    setBinds(result);
  });

  const sendBindMessage = useCallback(async (bind) => {
    if (await botListRef.current.getIsRandom()) {
      await botListRef.current.nextButton()
    }
    const bot = await botListRef.current.getSelectedBot();
    if (bot === null) {
      showNotification("Выберите бота", "error");
      return;
    }
    const auth_token = Cookies.get('auth_token');
    const response = await fetch('/api/app/sendbindmessage', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': auth_token
      },
      body: JSON.stringify({
        botname: bot.username,
        bindname: bind.title
      })
    });
      if (response.redirected) {
          window.location.href = response.url;
      }

    const result = await response.json();

    if (result.status !== "ok") {
      showNotification(result.message, "error");
      return;
    }
  });

  useEffect(() => {
    if (isSent || keys.length === 0) return;
    binds.forEach((bind) => {
      if (bind.hotKeys === null) return;
      if (bind.hotKeys.length === keys.length && bind.hotKeys.every((key, index) => key === keys[index])) {
        sendBindMessage(bind);
      }
    });
  }, [keys]);

  useEffect(() => {
    getUser();
    getBinds();
  }, []);

  const handleKeyDown = (event) => {
    if (event.target.tagName === 'INPUT' || event.target.tagName === 'TEXTAREA') {
      return;
    }
    event.preventDefault();
    setKeys(prev => {
      if (prev.includes(event.code)) return prev;
      return [...prev, event.code];
    });
  };

  const handleKeyUp = (event) => {
    if (!event.target.tagName === 'INPUT' || !event.target.tagName === 'TEXTAREA') {
      event.preventDefault();
    }
    setIsSent(false);
    setKeys(prev => prev.filter((key) => key !== event.code));
  };

  useEffect(() => {
    window.addEventListener('keydown', handleKeyDown);
    window.addEventListener('keyup', handleKeyUp);
    return () => {
      window.removeEventListener('keydown', handleKeyDown);
      window.removeEventListener('keyup', handleKeyUp);
    };
  }, []);

  useEffect(() => {
    const interval = setInterval(async () => {
      const auth_token = Cookies.get('auth_token');
      const response = await fetch('/api/app/ping', {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': auth_token
        }
      });
        if (response.redirected) {
            window.location.href = response.url;
        }
    }, 15000);
    return () => clearInterval(interval);
  }, []);

  useEffect(() => {
    if (user === null) return;
    editStreamerUsernameRef.current.setInputValue(user.streamerUsername);
    headerRef.current.setStreamerUsername(user.streamerUsername.length === 0 ? "-" : user.streamerUsername);
    getViewerCount();
    const interval = setInterval(getViewerCount, 5000);
    return () => clearInterval(interval);
  }, [user]);

  return (
    user !== null && (
      <FollowBotProvider>
        <BindsEditorProvider>
          <SpamEditorProvider>
            <div className={styles.main_container}>
              <DropdownContainer title="Боты" _isOpen={!isMobile} disabled={!isMobile} buttonStyle={{ height: "30px" }}>
                <BotList ref={botListRef} setBotsCount={headerRef.current.setBotsCount} isMobile={isMobile} />
              </DropdownContainer>
              <div className={styles.vertical_grid}>
                <DropdownContainer title="Стрим" _isOpen={true} disabled={!isMobile} buttonStyle={{ height: "30px" }}>
                  <Stream streamerUsername={user.streamerUsername} isOnline={streamOnline} />
                </DropdownContainer>
                <div className={styles.horizontal_grid}>
                  <BindsDropdownContainer callbackFunc={getBinds} title="Панель биндов" _isOpen={!isMobile} disabled={!isMobile} buttonStyle={{ height: "30px" }}>
                    <Binds binds={binds} sendBindMessage={sendBindMessage} />
                  </BindsDropdownContainer>
                  <DropdownContainer title="Спам" _isOpen={!isMobile} disabled={!isMobile} buttonStyle={{ height: "30px" }}>
                    <Spam />
                  </DropdownContainer>
                </div>
              </div>
              <div className={styles.vertical_grid}>
                <DropdownContainer title="Настройки стрима" _isOpen={!isMobile} disabled={!isMobile} buttonStyle={{ height: "30px" }}>
                  <EditStreamerUsername ref={editStreamerUsernameRef} placeholder="Ник стримера" callbackFunc={editStreamerUsername} />
                </DropdownContainer>
                {
                  user.username &&
                  <DropdownContainer title="Чат" _isOpen={true} disabled={!isMobile} buttonStyle={{ height: "30px" }}>
                    <Chat botListRef={botListRef} streamerUsername={user.streamerUsername} />
                  </DropdownContainer>
                }
              </div>
            </div>
          </SpamEditorProvider>
        </BindsEditorProvider>
      </FollowBotProvider>
    )
  );
}

export default App;
