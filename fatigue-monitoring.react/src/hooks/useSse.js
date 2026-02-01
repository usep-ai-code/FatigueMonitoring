import { useState, useEffect, useRef, useCallback } from 'react';
import { SSE_STATUS } from '../types/dashboard';
import { API_CONFIG, getApiUrl } from '../services/config';

/**
 * Custom hook for SSE (Server-Sent Events) connection
 * Handles connection, reconnection, heartbeat, and data updates
 */
export function useSse() {
  const [status, setStatus] = useState(SSE_STATUS.DISCONNECTED);
  const [data, setData] = useState(null);
  const [error, setError] = useState(null);
  const [lastHeartbeat, setLastHeartbeat] = useState(null);
  
  const eventSourceRef = useRef(null);
  const reconnectTimeoutRef = useRef(null);
  const heartbeatTimeoutRef = useRef(null);
  
  const RECONNECT_DELAY = 5000; // 5 seconds
  const HEARTBEAT_TIMEOUT = 60000; // 60 seconds - if no heartbeat received

  const clearTimeouts = useCallback(() => {
    if (reconnectTimeoutRef.current) {
      clearTimeout(reconnectTimeoutRef.current);
      reconnectTimeoutRef.current = null;
    }
    if (heartbeatTimeoutRef.current) {
      clearTimeout(heartbeatTimeoutRef.current);
      heartbeatTimeoutRef.current = null;
    }
  }, []);

  const disconnect = useCallback(() => {
    clearTimeouts();
    if (eventSourceRef.current) {
      eventSourceRef.current.close();
      eventSourceRef.current = null;
    }
    setStatus(SSE_STATUS.DISCONNECTED);
  }, [clearTimeouts]);

  const resetHeartbeatTimer = useCallback(() => {
    if (heartbeatTimeoutRef.current) {
      clearTimeout(heartbeatTimeoutRef.current);
    }
    heartbeatTimeoutRef.current = setTimeout(() => {
      console.warn('Heartbeat timeout - connection may be stale');
      // Connection might be stale, try to reconnect
      disconnect();
    }, HEARTBEAT_TIMEOUT);
  }, [disconnect]);

  const connect = useCallback(() => {
    // Don't connect if already connecting or connected
    if (eventSourceRef.current) {
      return;
    }

    setStatus(SSE_STATUS.CONNECTING);
    setError(null);

    try {
      const url = getApiUrl(API_CONFIG.ENDPOINTS.DASHBOARD_STREAM);
      console.log('Connecting to SSE:', url);
      
      const eventSource = new EventSource(url);
      eventSourceRef.current = eventSource;

      // Handle connection opened
      eventSource.onopen = () => {
        console.log('SSE connection opened');
        setStatus(SSE_STATUS.CONNECTED);
        setError(null);
        resetHeartbeatTimer();
      };

      // Handle generic messages
      eventSource.onmessage = (event) => {
        console.log('SSE message received:', event.data);
        resetHeartbeatTimer();
      };

      // Handle 'connected' event
      eventSource.addEventListener('connected', (event) => {
        console.log('SSE connected event:', event.data);
        setStatus(SSE_STATUS.CONNECTED);
        resetHeartbeatTimer();
      });

      // Handle 'heartbeat' event
      eventSource.addEventListener('heartbeat', (event) => {
        try {
          const heartbeatData = JSON.parse(event.data);
          setLastHeartbeat(new Date(heartbeatData.data?.serverTime || Date.now()));
          resetHeartbeatTimer();
        } catch (e) {
          console.error('Error parsing heartbeat:', e);
        }
      });

      // Handle 'dashboard_update' event
      eventSource.addEventListener('dashboard_update', (event) => {
        try {
          const eventData = JSON.parse(event.data);
          if (eventData.data) {
            setData(eventData.data);
          }
          resetHeartbeatTimer();
        } catch (e) {
          console.error('Error parsing dashboard update:', e);
        }
      });

      // Handle errors
      eventSource.onerror = (event) => {
        console.error('SSE error:', event);
        setStatus(SSE_STATUS.DISCONNECTED);
        setError('Connection lost');
        
        // Close the connection
        eventSource.close();
        eventSourceRef.current = null;
        clearTimeouts();

        // Schedule reconnection
        reconnectTimeoutRef.current = setTimeout(() => {
          console.log('Attempting to reconnect...');
          connect();
        }, RECONNECT_DELAY);
      };

    } catch (err) {
      console.error('Error creating EventSource:', err);
      setStatus(SSE_STATUS.DISCONNECTED);
      setError(err.message);
      
      // Schedule reconnection
      reconnectTimeoutRef.current = setTimeout(() => {
        connect();
      }, RECONNECT_DELAY);
    }
  }, [resetHeartbeatTimer, clearTimeouts]);

  // Auto-connect on mount
  useEffect(() => {
    connect();

    // Cleanup on unmount
    return () => {
      disconnect();
    };
  }, [connect, disconnect]);

  // Manual reconnect function
  const reconnect = useCallback(() => {
    disconnect();
    setTimeout(connect, 100);
  }, [disconnect, connect]);

  return {
    status,
    data,
    error,
    lastHeartbeat,
    connect,
    disconnect,
    reconnect,
    isConnected: status === SSE_STATUS.CONNECTED,
    isConnecting: status === SSE_STATUS.CONNECTING
  };
}

export default useSse;
