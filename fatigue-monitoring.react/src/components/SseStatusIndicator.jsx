import React from 'react';
import { Wifi, WifiOff, RefreshCw } from 'lucide-react';
import { SSE_STATUS } from '../types/dashboard';

/**
 * SSE Connection Status Indicator Component
 * Displays connection status with color indicators:
 * - Green: Connected
 * - Yellow: Connecting
 * - Red: Disconnected
 */
export function SseStatusIndicator({ 
  status, 
  lastHeartbeat, 
  onReconnect, 
  darkMode = true,
  showText = true 
}) {
  const getStatusConfig = () => {
    switch (status) {
      case SSE_STATUS.CONNECTED:
        return {
          color: 'bg-emerald-500',
          pulseClass: 'sse-connected',
          text: 'Connected',
          textColor: 'text-emerald-500',
          icon: Wifi
        };
      case SSE_STATUS.CONNECTING:
        return {
          color: 'bg-yellow-500',
          pulseClass: 'sse-connecting',
          text: 'Connecting...',
          textColor: 'text-yellow-500',
          icon: RefreshCw
        };
      case SSE_STATUS.DISCONNECTED:
      default:
        return {
          color: 'bg-red-500',
          pulseClass: 'sse-disconnected',
          text: 'Disconnected',
          textColor: 'text-red-500',
          icon: WifiOff
        };
    }
  };

  const config = getStatusConfig();
  const IconComponent = config.icon;

  const formatLastHeartbeat = () => {
    if (!lastHeartbeat) return null;
    const now = new Date();
    const diff = Math.floor((now - lastHeartbeat) / 1000);
    if (diff < 60) return `${diff}s ago`;
    return `${Math.floor(diff / 60)}m ago`;
  };

  return (
    <div 
      className={`flex items-center gap-3 px-4 py-2 rounded-full border transition-all cursor-pointer hover:opacity-80 ${
        darkMode 
          ? 'bg-slate-800 border-slate-700' 
          : 'bg-white border-slate-300 shadow-sm'
      }`}
      onClick={status === SSE_STATUS.DISCONNECTED ? onReconnect : undefined}
      title={status === SSE_STATUS.DISCONNECTED ? 'Click to reconnect' : `SSE Status: ${config.text}`}
    >
      {/* Status Dot */}
      <div className="relative">
        <div className={`w-3 h-3 rounded-full ${config.color} ${config.pulseClass}`} />
      </div>
      
      {/* Icon */}
      <IconComponent 
        size={18} 
        className={`${config.textColor} ${status === SSE_STATUS.CONNECTING ? 'animate-spin' : ''}`} 
      />
      
      {/* Text */}
      {showText && (
        <div className="flex flex-col">
          <span className={`text-sm font-semibold ${config.textColor}`}>
            {config.text}
          </span>
          {status === SSE_STATUS.CONNECTED && lastHeartbeat && (
            <span className={`text-xs ${darkMode ? 'text-slate-400' : 'text-slate-500'}`}>
              Last: {formatLastHeartbeat()}
            </span>
          )}
        </div>
      )}
      
      {/* Reconnect hint for disconnected state */}
      {status === SSE_STATUS.DISCONNECTED && (
        <RefreshCw 
          size={14} 
          className={`${darkMode ? 'text-slate-400' : 'text-slate-500'} ml-1`} 
        />
      )}
    </div>
  );
}

export default SseStatusIndicator;
