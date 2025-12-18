import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App'
import './index.css'

// ВАЖНО: Проверяем, что элемент root существует
const rootElement = document.getElementById('root');

if (rootElement) {
  ReactDOM.createRoot(rootElement).render(
    <React.StrictMode>
      <App />
    </React.StrictMode>,
  )
} else {
  console.error('НЕ НАЙДЕН корневой элемент с id="root" в index.html');
}
