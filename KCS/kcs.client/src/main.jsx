import React from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import Authorization from './pages/authorization/Authorization';
import Layout from './pages/layout/Layout';
import { NotificationsProvider } from './contexts/notification/NotificationsContext';
import { FileReaderProvider } from './contexts/fileReader/FileReaderContext';
import { FileDownloadProvider } from './contexts/fileDownload/FileDownloadContext';
import "./main.css";
import { Navigate } from 'react-router-dom';

ReactDOM.createRoot(document.getElementById('root')).render(
  <FileDownloadProvider>
    <FileReaderProvider>
      <NotificationsProvider>
        <Router>
          <Routes>
            <Route exact path="/signup" element={<Authorization />} />
            <Route exact path="/signin" element={<Authorization />} />
            <Route exact path="/app" element={<Layout />} />
            <Route exact path="/admin-panel" element={<Layout />} />
            <Route exact path="/invite-codes" element={<Layout />} />
            <Route exact path="/pause" element={<Layout />} />
            <Route path="/*" element={<Navigate to="/app" />} />
          </Routes>
        </Router>
      </NotificationsProvider>
    </FileReaderProvider>
  </FileDownloadProvider>
)
