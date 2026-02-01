import { useState, useEffect, useRef } from 'react';

export const useSSE = (url) => {
  const [data, setData] = useState(null);
  const [connectionStatus, setConnectionStatus] = useState('connecting'); // connecting, connected, disconnected
  const [error, setError] = useState(null);
  const eventSourceRef = useRef(null);
  const reconnectTimeoutRef = useRef(null);
  const reconnectAttemptsRef = useRef(0);
  const maxReconnectAttempts = 5;

  useEffect(() => {
    const connect = () => {
      try {
        setConnectionStatus('connecting');
        console.log('Attempting to connect to SSE:', url);
        
        const eventSource = new EventSource(url);
        eventSourceRef.current = eventSource;

        eventSource.addEventListener('connected', (event) => {
          console.log('SSE connected:', event.data);
          setConnectionStatus('connected');
          setError(null);
          reconnectAttemptsRef.current = 0;
        });

        eventSource.addEventListener('heartbeat', (event) => {
          // console.log('SSE heartbeat received');
          setConnectionStatus('connected');
        });

        eventSource.addEventListener('update', (event) => {
          try {
            const parsedData = JSON.parse(event.data);
            console.log('SSE data update received');
            setData(parsedData);
            setConnectionStatus('connected');
          } catch (err) {
            console.error('Error parsing SSE data:', err);
          }
        });

        eventSource.onerror = (err) => {
          console.error('SSE error:', err);
          setConnectionStatus('disconnected');
          setError('Connection lost');
          eventSource.close();

          // Attempt to reconnect with exponential backoff
          if (reconnectAttemptsRef.current < maxReconnectAttempts) {
            const delay = Math.min(1000 * Math.pow(2, reconnectAttemptsRef.current), 30000);
            console.log(`Reconnecting in ${delay}ms (attempt ${reconnectAttemptsRef.current + 1}/${maxReconnectAttempts})`);
            
            reconnectTimeoutRef.current = setTimeout(() => {
              reconnectAttemptsRef.current++;
              connect();
            }, delay);
          } else {
            console.error('Max reconnection attempts reached');
            setError('Unable to connect to server');
          }
        };

      } catch (err) {
        console.error('Error creating EventSource:', err);
        setConnectionStatus('disconnected');
        setError(err.message);
      }
    };

    connect();

    // Cleanup
    return () => {
      if (eventSourceRef.current) {
        eventSourceRef.current.close();
      }
      if (reconnectTimeoutRef.current) {
        clearTimeout(reconnectTimeoutRef.current);
      }
    };
  }, [url]);

  return { data, connectionStatus, error };
};
