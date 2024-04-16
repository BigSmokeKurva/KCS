import React from "react";
import { useState } from "react";
import Notification from "./Notification";
import styles from "./style.module.css";

const NotificationsContext = React.createContext();

function NotificationsProvider({ children }) {
  const [notifications, setNotifications] = useState([]);

  function generateNotificationId() {
    return `notification-${Math.floor(Math.random() * 1000000)}`;
  }

  function showNotification(text, type) {
    setNotifications(prevNotifications => [{ text, type, id: generateNotificationId() }, ...prevNotifications]);
  }

  return (
    <NotificationsContext.Provider value={{ showNotification }}>
      {notifications.length > 0 && (
        <div className={styles.notification_container}>
          {notifications.map((notification, index) => {
            return (
              <Notification
                top={index != 0 ? (31 * (index)) + 73 : 73}
                key={notification.id}
                id={notification.id}
                text={notification.text}
                type={notification.type}
                setNotifications={setNotifications}
              />
            );
          })}
        </div>
      )}
      {children}
    </NotificationsContext.Provider>
  );
}

export { NotificationsProvider, NotificationsContext };