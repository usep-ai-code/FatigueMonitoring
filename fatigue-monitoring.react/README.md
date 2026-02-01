# Fatigue Monitoring React Dashboard

Real-time fatigue monitoring dashboard built with React and Vite.

## Features

- **Real-time Updates**: Uses Server-Sent Events (SSE) for live data streaming
- **SSE Connection Status**: Visual indicator showing connection state
  - 🟢 Green - Connected
  - 🟡 Yellow - Connecting
  - 🔴 Red - Disconnected
- **Dark/Light Mode**: Toggle between themes
- **Responsive Design**: Optimized for command center displays
- **Notification Pop-ups**: Alerts for new fatigue events

## Quick Start

```bash
# Install dependencies
npm install

# Create environment file
cp .env.example .env

# Start development server
npm run dev
```

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `VITE_API_URL` | Backend API URL | `https://localhost:7001` |

## Project Structure

```
src/
├── components/
│   ├── SCCDashboard.jsx    # Main dashboard component
│   └── SseStatusIndicator.jsx  # SSE connection indicator
├── hooks/
│   └── useSse.js           # SSE connection hook
├── services/
│   └── config.js           # API configuration
├── types/
│   └── dashboard.js        # Type definitions
├── App.jsx
├── main.jsx
└── index.css
```

## Build for Production

```bash
npm run build
```

Output will be in `dist/` folder.

## Technology Stack

- React 19
- Vite 7
- Tailwind CSS 3
- Lucide React Icons
