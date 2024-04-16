import React, { createContext, useState } from 'react';

const FileDownloadContext = createContext();

const FileDownloadProvider = ({ children }) => {
  const [filename, setFilename] = useState('');
  const [data, setData] = useState('');

  const downloadFile = (filename, data) => {
    setFilename(filename);
    setData(data);

    const element = document.createElement('a');
    element.setAttribute('href', 'data:text/plain;charset=utf-8,' + encodeURIComponent(data));
    element.setAttribute('download', filename);
    document.body.appendChild(element);
    element.click();
    document.body.removeChild(element);
  };

  return (
    <FileDownloadContext.Provider value={{ downloadFile }}>
      {children}
    </FileDownloadContext.Provider>
  );
};

export { FileDownloadContext, FileDownloadProvider };
