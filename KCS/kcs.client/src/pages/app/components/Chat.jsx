import { useState, useEffect, useRef, useCallback, useContext } from 'react';
import styles from "../style.module.css";
import classNames from 'classnames';
import Cookies from 'js-cookie';
import { NotificationsContext } from '../../../contexts/notification/NotificationsContext';

function Chat({ botListRef, streamerUsername }) {
    const [messages, setMessages] = useState([]);
    const usernamesWithColors = useRef({});
    const [scrollAtBottom, setScrollAtBottom] = useState(true);
    const listContainerRef = useRef(null);
    const [isRandom, setIsRandom] = useState(false);
    const { showNotification } = useContext(NotificationsContext);
    const [inputValue, setInputValue] = useState("");
    const [replyMessage, setReplyMessage] = useState(null);
    const [lastMessages, setLastMessages] = useState([]);
    const [lastMessagesOpen, setLastMessagesOpen] = useState(false);
    const inputRef = useRef(null);

    useEffect(() => {
        if (streamerUsername === "" && !streamerUsername) {
            return;
        }
        const generateRandomNickname = () => {
            const randomNum = Math.floor(Math.random() * 1000000);
            return `justinfan${randomNum}`;
        };
        const websocket = new WebSocket('wss://irc-ws.chat.twitch.tv/');
        websocket.onopen = () => {
            const nickname = generateRandomNickname();
            websocket.send("CAP REQ :twitch.tv/tags twitch.tv/commands");
            websocket.send(`NICK ${nickname}`);
            websocket.send(`USER ${nickname} 8 * :${nickname}`);
            websocket.send(`JOIN #${streamerUsername}`);
        };

        websocket.onmessage = (e) => {
            if (e.data.includes("PING")) {
                websocket.send("PONG");
                return;
            }

            if (!e.data.includes("PRIVMSG")) {
                return;
            }
            const badgesRegex = /badges=([^;]+)/;
            const tmiSentTsRegex = /tmi-sent-ts=(\d+)/;
            const idRegex = /id=([^;]+)/;
            const usernameRegex = /display-name=([^;]+)/;
            const messageRegex = /PRIVMSG #[\w-]+ :(.+)/;

            const rolesMatch = e.data.match(badgesRegex);

            const roles = rolesMatch ? rolesMatch[1].split(",").map(badge => badge.split('/')[0]) : [];
            const time = new Date(parseInt(e.data.match(tmiSentTsRegex)[1]));
            const id = e.data.match(idRegex)[1];
            const username = e.data.match(usernameRegex)[1];
            const msg = e.data.match(messageRegex)[1];
            const colorIndex = usernamesWithColors[username] === undefined ? Math.floor(Math.random() * 20) + 1 : usernamesWithColors[username];
            if (usernamesWithColors[username] === undefined) {
                usernamesWithColors[username] = colorIndex;
            }

            const message = {
                time: time.toLocaleTimeString(),
                username,
                roles,
                text: msg,
                id,
                colorIndex,
            };
            setMessages((prev) => [...prev.slice(-99), message]);
        };

        return () => {
            websocket.close();
            setMessages([]);
            setReplyMessage(null);
        }
    }, [streamerUsername]);

    const handleScroll = (event) => {
        const element = event.currentTarget;
        const isAtBottom = element.scrollTop + element.clientHeight >= element.scrollHeight - 1;

        if (isAtBottom !== scrollAtBottom) {
            setScrollAtBottom(isAtBottom);
        }
    };

    const sendMessage = useCallback(async () => {
        const auth_token = Cookies.get("auth_token");
        if (isRandom) {
            await botListRef.current.nextButton()
        }
        const bot = botListRef.current.getSelectedBot();
        if (!bot) {
            if (isRandom) {
                showNotification("Нет подключенных ботов", "error");
                return;
            }
            showNotification("Не выбран бот", "error");
            return;
        }

        var data = {
            message: inputValue,
            botname: bot.username,
            replyTo: replyMessage ? replyMessage.id : null
        };
        if (inputValue === "") {
            showNotification("Невозможно отправить пустое сообщение", "error");
            return;
        }
        const response = await fetch("/api/app/sendmessage", {
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

        if (!lastMessages.includes(inputValue)) {
            setLastMessages((prev) => [...prev.slice(-4), inputValue]);
        }
        setInputValue("");
        replyMessage && setReplyMessage(null);
    });

    useEffect(() => {
        if (scrollAtBottom && listContainerRef.current) {
            listContainerRef.current.scrollTop = listContainerRef.current.scrollHeight;
        }
    }, [messages, scrollAtBottom]);

    function clickLastMessage(message) {
        setLastMessagesOpen(false);
        setInputValue(message);
        inputRef.current.focus();
    }

    return (
        <div className={styles.chat_container}>
            <div className={styles.list_container} ref={listContainerRef} onScroll={handleScroll}>
                <div className={styles.messages_container}>
                    {messages.map((message) => {
                        return (
                            <div key={message.id} className={styles.message_item}>
                                <div className={styles.message_body}>
                                    <span className={styles.message_time}>{message.time}</span>
                                    {
                                        message.roles.includes("broadcaster") &&
                                        <span className={classNames(
                                            styles.message_role,
                                            styles.broadcaster
                                        )}>B</span>
                                    }
                                    {
                                        message.roles.includes("moderator") &&
                                        <span className={classNames(
                                            styles.message_role,
                                            styles.moderator
                                        )}>М</span>
                                    }
                                    {
                                        message.roles.includes("vip") &&
                                        <span className={classNames(
                                            styles.message_role,
                                            styles.subscriber
                                        )}>V</span>
                                    }
                                    {
                                        (message.roles.includes("subscriber") && !message.roles.includes("broadcaster")) &&
                                        <span className={classNames(
                                            styles.message_role,
                                            styles.subscriber
                                        )}>S</span>
                                    }

                                    <span className={classNames(
                                        styles.message_username,
                                        styles[`color${message.colorIndex}`]

                                    )}>{message.username}<span>:</span></span>
                                    <span className={styles.message_text}>{message.text}</span>
                                </div>
                                <button className={styles.reply_message_button} onClick={() => { setReplyMessage(message) }}></button>
                            </div>
                        )
                    })}
                </div>
            </div>
            <div className={styles.chat_control_container}>
                {
                    (replyMessage && !lastMessagesOpen) ? (
                        <div className={styles.reply_message_container}>
                            <div>
                                <span>
                                    <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 20 20" fill="none">
                                        <path d="M7.49996 9.16659L4.16663 12.4999M4.16663 12.4999L7.49996 15.8333M4.16663 12.4999H13.3333C14.2173 12.4999 15.0652 12.1487 15.6903 11.5236C16.3154 10.8985 16.6666 10.0506 16.6666 9.16659C16.6666 8.28253 16.3154 7.43468 15.6903 6.80956C15.0652 6.18444 14.2173 5.83325 13.3333 5.83325H12.5" stroke="#EFEFF1" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
                                    </svg>
                                    В ответ @{replyMessage.username}:
                                </span>
                                <button className={styles.cancel_reply_message} onClick={() => setReplyMessage(null)}>
                                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 16 16" fill="none">
                                        <g clipPath="url(#clip0_822_678)">
                                            <path d="M15.1 3.0999L12.9 0.899902L7.99999 5.8999L3.09999 0.899902L0.899994 3.0999L5.89999 7.9999L0.899994 12.8999L3.09999 15.0999L7.99999 10.0999L12.9 15.0999L15.1 12.8999L10.1 7.9999L15.1 3.0999Z" fill="white" />
                                        </g>
                                        <defs>
                                            <clipPath id="clip0_822_678">
                                                <rect width="16" height="16" fill="white" />
                                            </clipPath>
                                        </defs>
                                    </svg>
                                </button>
                            </div>
                            <span>{replyMessage.text}</span>
                        </div>
                    ) : lastMessagesOpen && (
                        <div className={styles.last_messages_container}>
                            {
                                lastMessages.map((message, index) => {
                                    return (
                                        <button key={index} className={styles.last_message_item} onClick={() => clickLastMessage(message)}>
                                            <span>{(lastMessages.length - index)}.</span>
                                            <span>{message}</span>
                                        </button>
                                    )
                                })
                            }
                        </div>
                    )
                }
                <input className={styles.chat_input}
                    type="text"
                    placeholder="Сообщение"
                    value={inputValue}
                    ref={inputRef}
                    onChange={(e) => setInputValue(e.target.value)}
                    onKeyUp={(e) => {
                        if (e.key === "Enter") {
                            sendMessage();
                        }
                    }}
                />
                <div className={styles.chat_buttons}>
                    <div className={styles.manage_bots_buttons}>
                        <button className={styles.navigate_bot_button} onClick={() => { botListRef.current.prevButton() }}>
                            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="21" viewBox="0 0 20 21" fill="none">
                                <path d="M5.505 11.3712L13.4588 15.9862C14.1338 16.3774 15 15.9037 15 15.1149V5.88495C15 5.09745 14.135 4.62245 13.4588 5.01495L5.505 9.62995C5.35145 9.7176 5.22382 9.84431 5.13504 9.99721C5.04627 10.1501 4.99951 10.3238 4.99951 10.5006C4.99951 10.6774 5.04627 10.851 5.13504 11.0039C5.22382 11.1568 5.35145 11.2835 5.505 11.3712Z" fill="white" />
                            </svg>
                        </button>
                        <button className={classNames(
                            styles.navigate_bot_button,
                            styles.random_bot_button,
                            isRandom && styles.on
                        )} onClick={() => {
                            botListRef.current.setIsRandom(!isRandom);
                            setIsRandom(!isRandom);
                        }}>
                            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="21" viewBox="0 0 20 21" fill="none">
                                <g clipPath="url(#clip0_740_2134)">
                                    <path d="M19.7254 14.5246C20.0916 14.8907 20.0916 15.4843 19.7254 15.8504L16.6004 18.9748C16.0141 19.5611 15 19.1502 15 18.3118V16.75H12.7037C12.6394 16.75 12.5758 16.7368 12.5169 16.7112C12.4579 16.6855 12.4049 16.6481 12.361 16.6011L9.60492 13.6481L11.6882 11.416L13.75 13.625H15V12.0632C15 11.2258 16.0134 10.8133 16.6004 11.4003L19.7254 14.5246ZM0.46875 7.375H3.75L5.81176 9.58402L7.89508 7.35187L5.13898 4.3989C5.09513 4.35191 5.04207 4.31444 4.98312 4.28883C4.92416 4.26321 4.86057 4.24999 4.79629 4.25H0.46875C0.209883 4.25 0 4.45988 0 4.71875V6.90625C0 7.16512 0.209883 7.375 0.46875 7.375ZM15 7.375V8.93687C15 9.77515 16.0141 10.1861 16.6004 9.5998L19.7254 6.47543C20.0916 6.10929 20.0916 5.5157 19.7254 5.14961L16.6004 2.02535C16.0134 1.43832 15 1.85086 15 2.68824V4.25H12.7037C12.6394 4.25 12.5758 4.26323 12.5169 4.28884C12.4579 4.31446 12.4049 4.35192 12.361 4.3989L3.75 13.625H0.46875C0.209883 13.625 0 13.8349 0 14.0937V16.2812C0 16.5401 0.209883 16.75 0.46875 16.75H4.79629C4.92621 16.75 5.05031 16.6961 5.13898 16.6011L13.75 7.375H15Z" fill="white" />
                                </g>
                                <defs>
                                    <clipPath id="clip0_740_2134">
                                        <rect width="20" height="20" fill="white" transform="translate(0 0.5)" />
                                    </clipPath>
                                </defs>
                            </svg>
                        </button>
                        <button className={styles.navigate_bot_button} onClick={() => { botListRef.current.nextButton(); }}>
                            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="21" viewBox="0 0 20 21" fill="none">
                                <path d="M14.495 11.3712L6.54125 15.9862C5.86625 16.3774 5 15.9037 5 15.1149V5.88495C5 5.09745 5.865 4.62245 6.54125 5.01495L14.495 9.62995C14.6485 9.7176 14.7762 9.84431 14.865 9.99721C14.9537 10.1501 15.0005 10.3238 15.0005 10.5006C15.0005 10.6774 14.9537 10.851 14.865 11.0039C14.7762 11.1568 14.6485 11.2835 14.495 11.3712Z" fill="white" />
                            </svg>
                        </button>
                    </div>
                    <div className={styles.manage_messages_buttons}>
                        <button className={styles.last_messages_button} onClick={() => setLastMessagesOpen(!lastMessagesOpen)}>
                            <svg xmlns="http://www.w3.org/2000/svg" width="20" height="21" viewBox="0 0 20 21" fill="none">
                                <g clipPath="url(#clip0_740_2142)">
                                    <path fillRule="evenodd" clipRule="evenodd" d="M2.17827 5.02167C2.27289 4.79323 2.43313 4.59799 2.63872 4.46063C2.84431 4.32327 3.08602 4.24997 3.33327 4.25H11.6666C13.3242 4.25 14.9139 4.90848 16.086 6.08058C17.2581 7.25268 17.9166 8.8424 17.9166 10.5C17.9166 12.1576 17.2581 13.7473 16.086 14.9194C14.9139 16.0915 13.3242 16.75 11.6666 16.75H4.16661C3.83509 16.75 3.51715 16.6183 3.28272 16.3839C3.0483 16.1495 2.91661 15.8315 2.91661 15.5C2.91661 15.1685 3.0483 14.8505 3.28272 14.6161C3.51715 14.3817 3.83509 14.25 4.16661 14.25H11.6666C12.6612 14.25 13.615 13.8549 14.3183 13.1516C15.0215 12.4484 15.4166 11.4946 15.4166 10.5C15.4166 9.50544 15.0215 8.55161 14.3183 7.84835C13.615 7.14509 12.6612 6.75 11.6666 6.75H6.35077L7.13411 7.53333C7.3617 7.76919 7.48754 8.08501 7.48454 8.41275C7.48154 8.7405 7.34993 9.05395 7.11806 9.2856C6.88619 9.51726 6.57261 9.64857 6.24486 9.65126C5.91711 9.65395 5.60142 9.52781 5.36577 9.3L2.44911 6.38333C2.27443 6.20855 2.15547 5.98592 2.10727 5.74356C2.05906 5.5012 2.08377 5.24999 2.17827 5.02167Z" fill="#EFEFF1" />
                                </g>
                                <defs>
                                    <clipPath id="clip0_740_2142">
                                        <rect width="20" height="20" fill="white" transform="translate(0 0.5)" />
                                    </clipPath>
                                </defs>
                            </svg>
                        </button>
                        <button className={styles.send_message_button} onClick={sendMessage}>Отправить</button>
                    </div>
                </div>
            </div>
        </div>
    )
}

export default Chat;