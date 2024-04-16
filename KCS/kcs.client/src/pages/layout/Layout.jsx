import Header from '../shared_components/Header';
import { useLocation } from 'react-router-dom';
import App from '../app/App';
import AdminPanel from '../admin_panel/AdminPanel';
import Pause from '../pause/Pause';
import { useRef, useEffect, useState, useCallback } from 'react';

function Layout() {
  const location = useLocation();
  const page = location.pathname.replace("/", "");
  const ref = useRef(null);
  const [isMobile, setIsMobile] = useState(false);

  const handleResize = useCallback(() => {
    if (window.innerWidth <= 768 && !isMobile) {
      setIsMobile(true);
    } else if (window.innerWidth > 768 && isMobile) {
      setIsMobile(false);
    }
  }, [isMobile]);


  useEffect(() => {
    window.addEventListener("resize", handleResize);
    return () => {
      window.removeEventListener("resize", handleResize);
    }
  }, [handleResize]);

  useEffect(() => {
    handleResize();
  }, []);

  return (
    <>
      <Header ref={ref} page={page} isMobile={isMobile} />
      {
        page === "app" ? <App headerRef={ref} isMobile={isMobile} /> :
          page === "admin-panel" || page === "invite-codes" ?
            <AdminPanel isMobile={isMobile} /> :
            page === "pause" ? <Pause /> : null
      }
    </>
  );
}

export default Layout;