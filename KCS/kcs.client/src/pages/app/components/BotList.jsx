import styles from "../style.module.css";
import { useState, useEffect, useCallback, useContext, forwardRef, useImperativeHandle, useRef } from "react";
import Cookies from "js-cookie";
import classNames from "classnames";
import MassActionsDropdown from "./MassActionsDropdown";
import BotActionDropdown from "./BotActionDropdown";
import { NotificationsContext } from "../../../contexts/notification/NotificationsContext";
import { FollowBotContext } from "../contexts/FollowBotContext";

const BotList = forwardRef((props, ref) => {
    const [bots, setBots] = useState([]);
    const [selectedBot, setSelectedBot] = useState(null);
    const [searchText, setSearchText] = useState("");
    const { showNotification } = useContext(NotificationsContext);
    const { openFollowBotOptions } = useContext(FollowBotContext);
    const [isRandom, setIsRandom] = useState(false);
    const [lastBots, setLastBots] = useState([]);
    const [currentIndexBot, setCurrentIndexBot] = useState(0);
    const selectedBotRef = useRef(selectedBot);
    const isButtonSelect = useRef(false);

    useImperativeHandle(ref, () => ({
        getBots: () => bots,
        setBots: (newBots) => setBots(newBots),
        setIsRandom: (value) => setIsRandom(value),
        getIsRandom: () => isRandom,
        nextButton: () => {
            isButtonSelect.current = true;
            _nextButton();
        },
        prevButton: () => {
            isButtonSelect.current = true;
            _prevButton();
        },
        getSelectedBot: () => selectedBot,
    }));


    const _nextButton = useCallback(() => {
        if (isRandom) {
            const connectedBots = bots.filter(bot => bot.isConnected && bot !== selectedBot);
            if (connectedBots.length === 0) {
                return;
            }
            const randomBot = connectedBots[Math.floor(Math.random() * connectedBots.length)];
            if (currentIndexBot !== lastBots.length - 1 && lastBots.length !== 0) {
                setSelectedBot(randomBot);
                setLastBots([...lastBots.slice(-9), randomBot]);
                setCurrentIndexBot(lastBots.length - 1);
                return;
            }
            setLastBots([...lastBots.slice(-9), randomBot]);
            setCurrentIndexBot(currentIndexBot === 9 ? 9 : lastBots.length === 0 ? 0 : currentIndexBot + 1);
            setSelectedBot(randomBot);
            return;
        }

        if (currentIndexBot !== lastBots.length - 1 && lastBots.length !== 0) {
            setSelectedBot(lastBots[currentIndexBot + 1]);
            setCurrentIndexBot(currentIndexBot + 1);
            return;
        }

        const index = bots.indexOf(selectedBot);
        if (index === -1) {
            setSelectedBot(bots[0]);
            setLastBots([...lastBots.slice(-9), bots[0]]);
            setCurrentIndexBot(0);
            return;
        }
        if (index === bots.length - 1) {
            setSelectedBot(bots[0]);
            setLastBots([...lastBots.slice(-9), bots[0]]);
            setCurrentIndexBot(currentIndexBot === 9 ? 9 : currentIndexBot + 1);
            return;
        }
        setSelectedBot(bots[index + 1]);
        setLastBots([...lastBots.slice(-9), bots[index + 1]]);
        setCurrentIndexBot(currentIndexBot === 9 ? 9 : currentIndexBot + 1);
    });

    function _prevButton() {
        if (currentIndexBot === 0) {
            return;
        }

        setCurrentIndexBot(currentIndexBot - 1);
        setSelectedBot(lastBots[currentIndexBot - 1]);
    }

    function selectBotButton(bot) {
        setSelectedBot(bot);
        setCurrentIndexBot(lastBots.length);
        setLastBots([...lastBots.slice(-9), bot]);
    }

    const getBots = useCallback(async () => {
        const auth_token = Cookies.get("auth_token");
        const response = await fetch("/api/app/getbots", {
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
        setBots(data);
        props.setBotsCount(data.length);
    });

    useEffect(() => {
        getBots();
    }, []);

    useEffect(() => {
        const hasQueueBot = bots.some(bot => bot.isQueue);
        if (hasQueueBot) {
            const timeout = setTimeout(() => {
                getBots();
            }, 10000);
            return () => clearTimeout(timeout);
        }
    }, [bots]);

    useEffect(() => {
        if(props.isMobile) return;
        if (selectedBotRef.current && isButtonSelect.current) {
            selectedBotRef.current.scrollIntoView({
                behavior: "smooth",
                block: "center",
            });
            isButtonSelect.current = false;
        }
    }, [selectedBot]);

    const massConnect = useCallback(async () => {
        showNotification("Массовое подключение запущено", "warning");
        setBots(bots.map(bot => ({ ...bot, isConnected: "queue" })));
        const auth_token = Cookies.get("auth_token");
        const response = await fetch("/api/app/connectallbots", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Authorization": auth_token,
            }
        });
        if (response.redirected) {
            window.location.href = response.url;
        }
        const data = await response.json();

        if (data.status !== "ok") {
            showNotification(data.message, "error");
            getBots();
            return;
        }
        showNotification("Массовое подключение завершено", "success");
        getBots();
    });

    const massDisconnect = useCallback(async () => {
        showNotification("Массовое отключение запущено", "warning");
        setBots(bots.map(bot => ({ ...bot, isConnected: "queue" })));
        const auth_token = Cookies.get("auth_token");
        const response = await fetch("/api/app/disconnectallbots", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Authorization": auth_token,
            }
        });
        if (response.redirected) {
            window.location.href = response.url;
        }
        const data = await response.json();

        if (data.status !== "ok") {
            showNotification(data.message, "error");
            return;
        }
        showNotification("Массовое отключение завершено", "success");
        getBots();
    });

    const massFollow = useCallback(async () => {
        openFollowBotOptions({
            _callbackFunc: async (delay) => {
                const auth_token = Cookies.get("auth_token");
                const response = await fetch("/api/app/followallbots", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "Authorization": auth_token,
                    },
                    body: JSON.stringify({ delay })
                });
                if (response.redirected) {
                    window.location.href = response.url;
                }
                const data = await response.json();

                if (data.status !== "ok") {
                    showNotification(data.message, "error");
                    return;
                }
                showNotification("Все аккаунты добавлены в очередь", "success");
                getBots();
            }
        });
    });

    const massUnfollow = useCallback(async () => {
        openFollowBotOptions({
            _callbackFunc: async (delay) => {
                const auth_token = Cookies.get("auth_token");
                const response = await fetch("/api/app/unfollowallbots", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "Authorization": auth_token,
                    },
                    body: JSON.stringify({ delay })
                });
                if (response.redirected) {
                    window.location.href = response.url;
                }
                const data = await response.json();

                if (data.status !== "ok") {
                    showNotification(data.message, "error");
                    return;
                }
                showNotification("Все аккаунты добавлены в очередь", "success");
                getBots();
            }
        });
    });

    const massDeleteQueue = useCallback(async () => {
        const auth_token = Cookies.get("auth_token");
        const response = await fetch("/api/app/followAllBotsCancel", {
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

        if (data.status !== "ok") {
            showNotification(data.message, "error");
            return;
        }
        showNotification("Очередь удалена", "success");
        getBots();
    });

    const massGetTags = useCallback(async () => {
        showNotification("Массовое получение тегов запущено", "warning");
        const auth_token = Cookies.get("auth_token");
        const response = await fetch("/api/app/getalltags", {
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

        if (data.status !== "ok") {
            showNotification(data.message, "error");
            return;
        }
        showNotification("Массовое получение тегов завершено", "success");
        getBots();
    });

    const connectBot = useCallback(async (bot) => {
        const auth_token = Cookies.get("auth_token");
        const response = await fetch("/api/app/connectbot", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Authorization": auth_token,
            },
            body: JSON.stringify({
                botUsername: bot.username
            })
        });
        if (response.redirected) {
            window.location.href = response.url;
        }
        const data = await response.json();

        if (data.status !== "ok") {
            showNotification(data.message, "error");
            return;
        }
        getBots();
    });

    const disconnectBot = useCallback(async (bot) => {
        const auth_token = Cookies.get("auth_token");
        const response = await fetch("/api/app/disconnectbot", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "Authorization": auth_token,
            },
            body: JSON.stringify({
                botUsername: bot.username
            })
        });
        if (response.redirected) {
            window.location.href = response.url;
        }
        const data = await response.json();

        if (data.status !== "ok") {
            showNotification(data.message, "error");
            return;
        }
        getBots();
    });

    const followBot = useCallback(async (bot) => {
        const auth_token = Cookies.get("auth_token");
        const response = await fetch("/api/app/followbot?botname=" + bot.username, {
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

        if (data.status !== "ok") {
            showNotification(data.message, "error");
            return;
        }
        getBots();
    });

    const unfollowBot = useCallback(async (bot) => {
        const auth_token = Cookies.get("auth_token");
        const response = await fetch("/api/app/unfollowbot?botname=" + bot.username, {
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

        if (data.status !== "ok") {
            showNotification(data.message, "error");
            return;
        }
        getBots();
    });

    const removeFromQueueBot = useCallback(async (bot) => {
        const auth_token = Cookies.get("auth_token");
        const response = await fetch("/api/app/followbotcancel?botname=" + bot.username, {
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

        if (data.status !== "ok") {
            showNotification(data.message, "error");
            return;
        }
        getBots();
    });

    return (
        <div className={styles.bots_list_container}>
            <div className={styles.search_container}>
                <input className={styles.search_input} type="text" placeholder="Поиск" onKeyUp={(event) => setSearchText(event.target.value.toLowerCase())} />
                <MassActionsDropdown
                    massConnect={massConnect}
                    massDisconnect={massDisconnect}
                    massFollow={massFollow}
                    massUnfollow={massUnfollow}
                    massDeleteQueue={massDeleteQueue}
                    massGetTags={massGetTags}
                />
            </div>
            <div className={styles.list_container}>
                <div className={styles.bots_list}>
                    {
                        bots.map((bot, index) => {
                            return (
                                <div key={bot.username} disabled={!bot.username.toLowerCase().includes(searchText)} className={classNames(
                                    styles.bot_item,
                                    selectedBot?.username === bot.username && styles.selected
                                )} ref={selectedBot?.username === bot.username ? selectedBotRef : undefined} onClick={() => selectBotButton(bot)}>
                                    <div className={styles.bot_username}>
                                        <span>{index + 1}.</span>
                                        {bot.username}
                                        {
                                            bot.tags.map((tag) => {
                                                var tagTitle = tag.substring(0, 3);
                                                return (
                                                    <span key={tag} className={classNames(
                                                        styles.tag,
                                                        styles[tag]
                                                    )}>{tagTitle.toUpperCase()}</span>
                                                )
                                            })
                                        }
                                    </div>
                                    <div className={styles.icon_container}>
                                        <span className={classNames(
                                            bot.isQueue ? styles.queue : bot.isFollowed ? styles.followed : styles.not_followed,
                                            styles.follow_icon,
                                            styles.icon
                                        )}
                                        ></span>
                                        <span className={classNames(
                                            bot.isConnected === "queue" ? styles.queue : bot.isConnected ? styles.connected : styles.disconnected,
                                            styles.connect_icon,
                                            styles.icon
                                        )}
                                        ></span>
                                        <BotActionDropdown bot={bot}
                                            connectBot={connectBot}
                                            disconnectBot={disconnectBot}
                                            followBot={followBot}
                                            unfollowBot={unfollowBot}
                                            removeFromQueueBot={removeFromQueueBot}
                                            isMobile={props.isMobile}
                                        />
                                    </div>

                                </div>
                            )
                        })
                    }
                </div>
            </div>
        </div>
    )
});

export default BotList;