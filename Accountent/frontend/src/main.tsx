import { createRoot } from 'react-dom/client';
import App from './app/App';
import './styles/index.css';
import { env } from './config/env';

document.title = env.appTitle;

createRoot(document.getElementById('root')!).render(<App />);
