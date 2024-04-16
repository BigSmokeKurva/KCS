import React, { forwardRef } from 'react';
import styles from './Checkbox.module.css';

const Checkbox = forwardRef((props, ref) => {
  return (
    <div className={styles.checkbox} onClick={props.onClick} role="checkbox" tabIndex={0}>
      {props.checked ? (
        <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 18 18" fill="none">
          <path d="M0 4C0 1.79086 1.79086 0 4 0H14C16.2091 0 18 1.79086 18 4V14C18 16.2091 16.2091 18 14 18H4C1.79086 18 0 16.2091 0 14V4Z" fill="#9147FF" />
          <path d="M7 13.4L3 9.4L4.4 8L7 10.6L13.6 4L15 5.4L7 13.4Z" fill="#330C6E" />
        </svg>
      ) : (
        <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 18 18" fill="none">
          <path fillRule="evenodd" clipRule="evenodd" d="M14 1H4C2.34315 1 1 2.34315 1 4V14C1 15.6569 2.34315 17 4 17H14C15.6569 17 17 15.6569 17 14V4C17 2.34315 15.6569 1 14 1ZM4 0C1.79086 0 0 1.79086 0 4V14C0 16.2091 1.79086 18 4 18H14C16.2091 18 18 16.2091 18 14V4C18 1.79086 16.2091 0 14 0H4Z" fill="#9147FF" />
        </svg>
      )}
      <input type="checkbox" ref={ref} checked={props.checked || false} readOnly />
    </div>
  );
});

export default Checkbox;
