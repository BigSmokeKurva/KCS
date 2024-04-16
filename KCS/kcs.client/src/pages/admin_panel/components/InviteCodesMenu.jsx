import { useState, useCallback, useEffect, forwardRef, useImperativeHandle, useRef } from "react";
import Cookies from "js-cookie";
import styles from "../style.module.css";
import classNames from "classnames";

const InviteCodesMenu = forwardRef(({ selectedCode, setSelectedCode, isMobile }, ref) => {
    const [codesList, setCodesList] = useState([]);
    const [searchText, setSearchText] = useState("");
    const selectedCodeRef = useRef(null);

    useEffect(() => {
        getCodes();
    }, [])

    useImperativeHandle(ref, () => ({
        addAndSelectCode: (code) => {
            setCodesList([code, ...codesList]);
            setSelectedCode(code);
            setTimeout(() => selectedCodeRef.current.scrollIntoView({ behavior: "smooth", block: "center" }), 0);
        },
        deleteCode: (code) => {
            setCodesList(codesList.filter(x => x.code !== code.code));
        }
    }));

    const getCodes = useCallback(async () => {
        var auth_token = Cookies.get("auth_token");
        const response = await fetch("/api/admin/getinvitecodes", {
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
        setCodesList(data);
    });

    function selectCode(code) {
        if (selectedCode.code === code.code) {
            return;
        }
        setSelectedCode(code);
    }

    function generateInviteCode() {
        const characters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
        let code = '';
        for (let i = 0; i < 11; i++) {
            code += characters.charAt(Math.floor(Math.random() * characters.length));
        }
        return code;
    }

    function createCode() {
        var code = {
            code: generateInviteCode(),
            status: "NotCreated",
            username: null,
            expires: null,
            activationDate: null,
            mode: "Time"
        };
        setSelectedCode(code);
    }

    return (
        <div className={styles.invite_menu_container}>
            {
                !isMobile &&
                <div className={styles.title}>
                    Инвайты
                </div>
            }
            <div className={styles.search_container}>
                <input className={styles.search_input} type="text" placeholder="Поиск" onKeyUp={(event) => setSearchText(event.target.value.toLowerCase())} />
                <button className={styles.create_invite_code_button} onClick={createCode}>Создать код</button>
            </div>
            <div className={styles.list_container}>
                <div className={styles.invite_list}>
                    {
                        codesList.filter(x => x.code.toLowerCase().includes(searchText)).map((item) => {
                            return (
                                <button className={classNames(
                                    styles.invite_code_list_item,
                                    selectedCode.code === item.code && styles.selected
                                )} ref={selectedCode.code === item.code ? selectedCodeRef : null} key={item.code} onClick={() => selectCode(item)}>
                                    <div>{item.code}</div>
                                    <svg width="8" height="9" viewBox="0 0 8 9" fill="none" xmlns="http://www.w3.org/2000/svg">
                                        <rect y="0.5" width="8" height="8" rx="4" fill={
                                            item.status === "Active" ? "#FA4" : item.status === "Used" ? "#6BB700" : "#C4314B"
                                        } />
                                    </svg>
                                </button>
                            )
                        })
                    }
                </div>
            </div>
        </div>
    )
});

export default InviteCodesMenu;