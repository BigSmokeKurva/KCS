import React, { createContext, useCallback } from 'react';

const FileReaderContext = createContext();

const FileReaderProvider = ({ children }) => {
  const readFile = useCallback((callback) => {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.txt';

    const handleFileRead = () => {
      const file = input.files[0];
      const reader = new FileReader();

      reader.onload = () => {
        callback(reader.result);
        input.remove();
      };

      reader.readAsText(file);
    };

    input.addEventListener('change', handleFileRead);

    input.dispatchEvent(new MouseEvent('click'));

    return () => {
      input.removeEventListener('change', handleFileRead);
      input.remove();
    };
  }, []);

  return (
    <FileReaderContext.Provider value={{ readFile }}>
      {children}
    </FileReaderContext.Provider>
  );
};

export { FileReaderContext, FileReaderProvider };
