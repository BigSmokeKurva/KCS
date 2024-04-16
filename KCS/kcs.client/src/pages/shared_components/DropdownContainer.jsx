import { useState, useRef, useEffect } from "react";
import styles from "./DropdownContainer.module.css";
import classNames from "classnames";

function DropdownContainer({ children, title, _isOpen, disabled, buttonStyle }) {
  const [isOpen, setIsOpen] = useState(_isOpen !== undefined ? _isOpen : false);
  const containerRef = useRef(null);
  const buttonRef = useRef(null);
  const contentRef = useRef(null);

  useEffect(() => {
    if (isOpen) {
      containerRef.current.style.removeProperty("height");
    }
  }, [isOpen])

  useEffect(() => {
    if (!isOpen && !disabled) {
      containerRef.current.style.height = buttonRef.current.offsetHeight + "px";
    }
  }, [])

  return (
    disabled ?
      <>
        <div
          className={classNames(styles.dropdown_container, !isOpen && styles.closed)}
          ref={containerRef}
        >
          <button ref={buttonRef} style={buttonStyle && buttonStyle} disabled={disabled}>
            {title}
            <svg xmlns="http://www.w3.org/2000/svg" width="21" height="21" viewBox="0 0 21 21" fill="none">
              <path d="M15 14.1001L10.5 9.6001L6 14.1001L4.5 12.6001L10.5 6.6001L16.5 12.6001L15 14.1001Z" fill="#EFEFF1" />
            </svg>
          </button>
          {children}
        </div>
      </> :
      <div
        className={classNames(styles.dropdown_container, !isOpen && styles.closed)}
        ref={containerRef}
        onTransitionEnd={() => {
          if (!isOpen) {
            containerRef.current.style.height = buttonRef.current.offsetHeight + "px";
          }
        }}
      >
        <button ref={buttonRef} onClick={() => setIsOpen(!isOpen)}>
          {title}
          <svg xmlns="http://www.w3.org/2000/svg" width="21" height="21" viewBox="0 0 21 21" fill="none">
            <path d="M15 14.1001L10.5 9.6001L6 14.1001L4.5 12.6001L10.5 6.6001L16.5 12.6001L15 14.1001Z" fill="#EFEFF1" />
          </svg>
        </button>
        <div ref={contentRef}>{children}</div>
      </div>
  );
}

export default DropdownContainer;
