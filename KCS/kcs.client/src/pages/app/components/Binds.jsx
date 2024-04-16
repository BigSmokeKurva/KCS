import styles from "../style.module.css";
import classNames from "classnames";

function Binds({ binds, sendBindMessage }) {
    return (
        <div className={classNames(
            styles.list_container,
            styles.binds_list_container
        )}>
            <div className={styles.binds_list}>
                {
                    binds.map((bind, index) => {
                        return (
                            <button key={index} className={styles.bind} onClick={() => sendBindMessage(bind)}>
                                {bind.title}
                            </button>
                        )
                    })
                }
            </div>
        </div>
    )
}

export default Binds;