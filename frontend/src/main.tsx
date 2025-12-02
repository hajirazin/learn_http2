import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

// Note: StrictMode removed because it's incompatible with long-running
// async streams (it mounts/unmounts/remounts which interrupts the stream)
createRoot(document.getElementById('root')!).render(<App />)
