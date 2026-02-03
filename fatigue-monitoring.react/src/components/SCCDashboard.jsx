import React, { useState, useEffect, useMemo, useRef, useCallback } from 'react';
import { 
  AlertTriangle, 
  CheckCircle, 
  Clock, 
  Map, 
  Activity, 
  Truck, 
  Zap, 
  Moon, 
  Sun,
  Radio,
  Users,
  Wifi,
  WifiOff,
  MapPin,
  ShieldAlert,
  Bell,
  X,
  LayoutGrid,
  Timer,
  Camera,
  EyeOff,
  Filter,
  ChevronLeft,
  ChevronRight,
  RefreshCw
} from 'lucide-react';
import { useSse } from '../hooks/useSse';
import { SseStatusIndicator } from './SseStatusIndicator';

const SCCDashboard = () => {
  const [darkMode, setDarkMode] = useState(true);
  const [currentTime, setCurrentTime] = useState(new Date());
  const [selectedArea, setSelectedArea] = useState('All'); 
  
  const [notifications, setNotifications] = useState([]);
  const [selectedAlert, setSelectedAlert] = useState(null);
  const [selectedLocationFilter, setSelectedLocationFilter] = useState(null);

  // SSE Hook for real-time data
  const { 
    status: sseStatus, 
    data: sseData, 
    lastHeartbeat,
    reconnect: sseReconnect,
    refetch,
    isConnected,
    isInitialLoading
  } = useSse();

  // Track previous alerts for notification detection
  const prevAlertsRef = useRef([]);

  // --- REFS FOR DYNAMIC PAGINATION ---
  const miningListContainerRef = useRef(null);
  const haulingListContainerRef = useRef(null);
  const activeFatigueListContainerRef = useRef(null);
  const recurrentListContainerRef = useRef(null);
  const highRiskListContainerRef = useRef(null); 

  // --- PAGINATION STATES ---
  const [miningPage, setMiningPage] = useState(1);
  const [haulingPage, setHaulingPage] = useState(1);
  const [delayedPage, setDelayedPage] = useState(1); 
  const [activeFatiguePage, setActiveFatiguePage] = useState(1);
  const [recurrentPage, setRecurrentPage] = useState(1);
  const [highRiskPage, setHighRiskPage] = useState(1);

  // --- DYNAMIC ITEMS PER PAGE STATE ---
  const [dynamicItemsPerPage, setDynamicItemsPerPage] = useState({
    mining: 4,
    hauling: 4,
    activeFatigue: 6,
    recurrent: 2,
    highRisk: 2
  });

  const ITEMS_DELAYED = 4;

  // --- EFFECT: Calculate Items Per Page based on container height ---
  useEffect(() => {
    const calculateCapacity = () => {
      const ITEM_HEIGHT_MINING_HAULING = 85; 
      const ITEM_HEIGHT_ACTIVE_FATIGUE = 130; 
      const ITEM_HEIGHT_RECURRENT = 60;
      const ITEM_HEIGHT_HIGH_RISK = 60;
      const PAGINATION_HEIGHT_BUFFER = 35;

      if (miningListContainerRef.current) {
        const height = miningListContainerRef.current.clientHeight;
        const availableHeight = height - PAGINATION_HEIGHT_BUFFER;
        const rows = Math.max(1, Math.floor(availableHeight / ITEM_HEIGHT_MINING_HAULING));
        setDynamicItemsPerPage(prev => ({ ...prev, mining: rows * 2 }));
      }

      if (haulingListContainerRef.current) {
        const height = haulingListContainerRef.current.clientHeight;
        const availableHeight = height - PAGINATION_HEIGHT_BUFFER;
        const rows = Math.max(1, Math.floor(availableHeight / ITEM_HEIGHT_MINING_HAULING));
        setDynamicItemsPerPage(prev => ({ ...prev, hauling: rows * 2 }));
      }

      if (activeFatigueListContainerRef.current) {
        const height = activeFatigueListContainerRef.current.clientHeight;
        const availableHeight = height - PAGINATION_HEIGHT_BUFFER;
        const rows = Math.max(1, Math.floor(availableHeight / ITEM_HEIGHT_ACTIVE_FATIGUE));
        setDynamicItemsPerPage(prev => ({ ...prev, activeFatigue: rows }));
      }

      // Recurrent and High Risk are fixed at 2 items max
      setDynamicItemsPerPage(prev => ({ ...prev, recurrent: 2, highRisk: 2 }));
    };

    calculateCapacity();
    window.addEventListener('resize', calculateCapacity);
    const timeoutId = setTimeout(calculateCapacity, 100);

    return () => {
      window.removeEventListener('resize', calculateCapacity);
      clearTimeout(timeoutId);
    };
  }, [selectedArea]); 

  // Reset pagination when area filter changes
  useEffect(() => {
    setMiningPage(1);
    setHaulingPage(1);
    setDelayedPage(1);
    setActiveFatiguePage(1);
    setRecurrentPage(1);
    setHighRiskPage(1);
  }, [selectedArea, selectedLocationFilter]);

  // Notification queue ref for staggered display
  const notificationQueueRef = useRef([]);
  const isProcessingQueueRef = useRef(false);
  const processTimeoutRef = useRef(null);

  // Stagger delay constants (milliseconds)
  const NOTIFICATION_STAGGER_DELAY = 200; // Delay between showing each notification
  const NOTIFICATION_AUTO_CLOSE_DELAY = 4000; // How long notification stays visible

  // Process notification queue - show one by one with stagger
  const processNotificationQueue = useCallback(() => {
    // Clear any pending process trigger
    if (processTimeoutRef.current) {
      clearTimeout(processTimeoutRef.current);
      processTimeoutRef.current = null;
    }

    // Already processing, let it continue
    if (isProcessingQueueRef.current) {
      return;
    }

    // Nothing to process
    if (notificationQueueRef.current.length === 0) {
      return;
    }

    isProcessingQueueRef.current = true;

    const processNext = () => {
      if (notificationQueueRef.current.length === 0) {
        isProcessingQueueRef.current = false;
        return;
      }

      const nextNotif = notificationQueueRef.current.shift();
      setNotifications(prev => [nextNotif, ...prev]);

      // Schedule auto-close
      setTimeout(() => {
        setNotifications(prev => prev.filter(n => n.id !== nextNotif.id));
      }, NOTIFICATION_AUTO_CLOSE_DELAY);

      // Process next notification after stagger delay
      if (notificationQueueRef.current.length > 0) {
        setTimeout(processNext, NOTIFICATION_STAGGER_DELAY);
      } else {
        isProcessingQueueRef.current = false;
      }
    };

    processNext();
  }, []);

  // Add notification to queue (debounced processing)
  const addNotification = useCallback((title, message, type = 'critical') => {
    const id = Date.now() + Math.random();
    const newNotif = { id, title, message, type };
    notificationQueueRef.current.push(newNotif);
    
    // Debounce: wait a tick to allow all sync additions to complete
    // before starting to process the queue
    if (!isProcessingQueueRef.current && !processTimeoutRef.current) {
      processTimeoutRef.current = setTimeout(() => {
        processTimeoutRef.current = null;
        processNotificationQueue();
      }, 10); // Small delay to batch all sync additions
    }
  }, [processNotificationQueue]);

  const removeNotification = useCallback((id) => {
    setNotifications(prev => prev.filter(n => n.id !== id));
  }, []);

  // Detect new alerts and show notifications (staggered)
  useEffect(() => {
    if (sseData?.activeAlerts) {
      const currentIds = new Set(sseData.activeAlerts.map(a => a.externalId));
      const prevIds = new Set(prevAlertsRef.current.map(a => a.externalId));
      
      // Find new alerts
      const newAlerts = sseData.activeAlerts.filter(a => !prevIds.has(a.externalId));
      
      // Queue notifications for each new alert (will be shown with stagger)
      newAlerts.forEach(alert => {
        addNotification(
          'New Fatigue Alert!',
          `Unit ${alert.unitName} detected in ${alert.location}`
        );
      });
      
      prevAlertsRef.current = sseData.activeAlerts;
    }
  }, [sseData?.activeAlerts, addNotification]);

  // Clock update - WITA (GMT+8)
  useEffect(() => {
    const updateWitaTime = () => {
      // Get current UTC time and add 8 hours for WITA
      const now = new Date();
      const utc = now.getTime() + (now.getTimezoneOffset() * 60000);
      const wita = new Date(utc + (8 * 60 * 60 * 1000)); // GMT+8
      setCurrentTime(wita);
    };
    
    updateWitaTime(); // Initial update
    const timer = setInterval(updateWitaTime, 1000);
    return () => clearInterval(timer);
  }, []);

  // --- COMPUTED DATA FROM SSE ---
  const stats = useMemo(() => {
    if (!sseData?.summary) {
      return { totalToday: 0, followedUpToday: 0, activeOpen: 0 };
    }
    
    // Apply global filter
    if (selectedArea === 'All') {
      return {
        totalToday: sseData.summary.totalAlarms,
        followedUpToday: sseData.summary.followedUp,
        activeOpen: sseData.summary.waitingFollowUp
      };
    }
    
    // Filter by selected area (Mining or Hauling)
    const areaData = selectedArea === 'Mining' 
      ? sseData.areaSummary?.mining 
      : sseData.areaSummary?.hauling;
    
    if (!areaData) {
      return { totalToday: 0, followedUpToday: 0, activeOpen: 0 };
    }
    
    return {
      totalToday: areaData.total || 0,
      followedUpToday: areaData.resolved || 0,
      activeOpen: areaData.open || 0
    };
  }, [sseData?.summary, sseData?.areaSummary, selectedArea]);

  const areaSummary = useMemo(() => {
    if (!sseData?.areaSummary) {
      return {
        Mining: { total: 0, open: 0, resolved: 0 },
        Hauling: { total: 0, open: 0, resolved: 0 }
      };
    }
    
    return {
      Mining: sseData.areaSummary.mining,
      Hauling: sseData.areaSummary.hauling
    };
  }, [sseData?.areaSummary]);

  const locationStats = useMemo(() => {
    const stats = { Mining: {}, Hauling: {} };
    
    if (sseData?.miningDistribution) {
      sseData.miningDistribution.forEach(item => {
        stats.Mining[item.location] = item.count;
      });
    }
    
    if (sseData?.haulingDistribution) {
      sseData.haulingDistribution.forEach(item => {
        stats.Hauling[item.location] = item.count;
      });
    }
    
    return stats;
  }, [sseData?.miningDistribution, sseData?.haulingDistribution]);

  const filteredAlerts = useMemo(() => {
    if (!sseData?.activeAlerts) return [];
    
    return sseData.activeAlerts
      .map(alert => {
        // Calculate dynamic duration based on current WITA time
        const dynamicDuration = (() => {
          if (!alert.eventTime) return 0;
          try {
            // EventTime from backend is in WITA (no timezone info)
            // Parse it and treat as WITA time
            let eventDate;
            const eventTimeStr = alert.eventTime;
            
            if (typeof eventTimeStr === 'string') {
              // Handle ISO format from JSON (e.g., "2026-02-01T09:11:58")
              // This is WITA time, so we need to parse it correctly
              // Remove any Z suffix and parse as local
              const cleanStr = eventTimeStr.replace('Z', '').replace('T', ' ');
              const parts = cleanStr.split(/[-: ]/);
              if (parts.length >= 6) {
                // Create date from parts (year, month-1, day, hour, min, sec)
                eventDate = new Date(
                  parseInt(parts[0]), parseInt(parts[1]) - 1, parseInt(parts[2]),
                  parseInt(parts[3]), parseInt(parts[4]), parseInt(parts[5])
                );
              } else {
                eventDate = new Date(eventTimeStr);
              }
            } else {
              eventDate = new Date(eventTimeStr);
            }
            
            if (isNaN(eventDate.getTime())) return alert.openDurationMinutes || 0;
            
            // currentTime is already in WITA, eventDate is now also treated as WITA
            const diffMs = currentTime.getTime() - eventDate.getTime();
            return Math.max(0, Math.floor(diffMs / (1000 * 60)));
          } catch {
            return alert.openDurationMinutes || 0;
          }
        })();
        
        return { ...alert, dynamicDurationMinutes: dynamicDuration };
      })
      .filter(alert => {
        // Must be Open status AND less than or equal to 30 minutes
        const isOpen = alert.status === 'Open';
        const isUnder30Min = alert.dynamicDurationMinutes <= 30;
        
        if (!isOpen || !isUnder30Min) return false;
        
        // Apply location filter if set
        if (selectedLocationFilter) {
          return alert.location === selectedLocationFilter;
        }
        
        // Apply area filter
        const areaMatch = selectedArea === 'All' || alert.area === selectedArea;
        return areaMatch;
      });
  }, [sseData?.activeAlerts, selectedArea, selectedLocationFilter, currentTime]);

  const overdueAlerts = useMemo(() => {
    if (!sseData?.delayedFollowUps) return [];
    
    // Calculate dynamic delay based on current WITA time
    const alertsWithDynamicDelay = sseData.delayedFollowUps.map(alert => {
      const dynamicDelay = (() => {
        if (!alert.eventTime) return alert.delayMinutes || 0;
        try {
          let eventDate;
          const eventTimeStr = alert.eventTime;
          
          if (typeof eventTimeStr === 'string') {
            const cleanStr = eventTimeStr.replace('Z', '').replace('T', ' ');
            const parts = cleanStr.split(/[-: ]/);
            if (parts.length >= 6) {
              eventDate = new Date(
                parseInt(parts[0]), parseInt(parts[1]) - 1, parseInt(parts[2]),
                parseInt(parts[3]), parseInt(parts[4]), parseInt(parts[5])
              );
            } else {
              eventDate = new Date(eventTimeStr);
            }
          } else {
            eventDate = new Date(eventTimeStr);
          }
          
          if (isNaN(eventDate.getTime())) return alert.delayMinutes || 0;
          
          const diffMs = currentTime.getTime() - eventDate.getTime();
          return Math.max(0, Math.floor(diffMs / (1000 * 60)));
        } catch {
          return alert.delayMinutes || 0;
        }
      })();
      
      return { ...alert, dynamicDelayMinutes: dynamicDelay };
    });
    
    if (selectedArea === 'All') {
      return alertsWithDynamicDelay;
    }
    return alertsWithDynamicDelay.filter(a => a.area === selectedArea);
  }, [sseData?.delayedFollowUps, selectedArea, currentTime]);

  const highRiskOperators = useMemo(() => {
    if (!sseData?.recurrentUnits) return [];
    
    if (selectedArea === 'All') {
      return sseData.recurrentUnits;
    }
    return sseData.recurrentUnits.filter(r => r.primaryArea === selectedArea);
  }, [sseData?.recurrentUnits, selectedArea]);

  const highFreqZones = useMemo(() => {
    if (!sseData?.highRiskAreas) return [];
    
    if (selectedArea === 'All') {
      return sseData.highRiskAreas;
    }
    return sseData.highRiskAreas.filter(h => h.area === selectedArea);
  }, [sseData?.highRiskAreas, selectedArea]);

  // Sensor Health - based on follow up status
  const sensorHealth = useMemo(() => {
    const total = stats.totalToday || 0;
    const followedUp = stats.followedUpToday || 0;
    const waitingFollowUp = stats.activeOpen || 0;
    const coverage = total > 0 ? Math.round((followedUp / total) * 100) : 0;
    
    return {
      total,
      followedUp,
      waitingFollowUp,
      coverage
    };
  }, [stats]);

  const handleAreaTabClick = (area) => {
    setSelectedArea(area);
    setSelectedLocationFilter(null);
  };

  // Helper Pagination Function
  const paginate = (data, page, limit) => {
    const start = (page - 1) * limit;
    return data.slice(start, start + limit);
  };

  // Helper Pagination Component
  const PaginationControls = ({ currentPage, totalPages, onPageChange }) => {
    if (totalPages <= 1) return null;
    return (
      <div className={`flex items-center justify-end gap-3 mt-auto pt-2 border-t border-dashed ${darkMode ? 'border-slate-700' : 'border-slate-300'}`}>
        <span className={`text-[clamp(0.8rem,1.2vh,1rem)] font-medium ${darkMode ? 'text-slate-200' : 'text-slate-500'}`}>
          Pg {currentPage}/{totalPages}
        </span>
        <div className="flex gap-2">
          <button 
            onClick={(e) => { e.stopPropagation(); onPageChange(Math.max(1, currentPage - 1)); }}
            disabled={currentPage === 1}
            className={`p-1.5 rounded hover:bg-slate-700 disabled:opacity-30 ${darkMode ? 'text-white' : 'text-slate-800'}`}
          >
            <ChevronLeft size={24} />
          </button>
          <button 
            onClick={(e) => { e.stopPropagation(); onPageChange(Math.min(totalPages, currentPage + 1)); }}
            disabled={currentPage === totalPages}
            className={`p-1.5 rounded hover:bg-slate-700 disabled:opacity-30 ${darkMode ? 'text-white' : 'text-slate-800'}`}
          >
            <ChevronRight size={24} />
          </button>
        </div>
      </div>
    );
  };

  const getCardBg = () => darkMode ? 'bg-slate-800 border-slate-700' : 'bg-slate-50 border-slate-300 shadow-sm';
  const getBodyBg = () => darkMode ? 'bg-slate-900 text-slate-100' : 'bg-slate-200 text-slate-800';
  const getAreaTitle = () => selectedArea === 'All' ? '' : `${selectedArea.toUpperCase()} `;

  return (
    <div className={`h-screen w-screen transition-colors duration-300 font-sans ${getBodyBg()} flex flex-col overflow-hidden relative selection:bg-red-500/30`}>
      
      {/* --- INITIAL LOADING OVERLAY --- */}
      {isInitialLoading && !sseData && (
        <div className={`absolute inset-0 z-[150] flex items-center justify-center ${darkMode ? 'bg-slate-900/95' : 'bg-slate-200/95'}`}>
          <div className="flex flex-col items-center gap-6">
            <div className="relative">
              <div className={`w-20 h-20 border-4 border-t-transparent rounded-full animate-spin ${darkMode ? 'border-blue-500' : 'border-blue-600'}`}></div>
              <div className={`absolute inset-0 w-20 h-20 border-4 border-b-transparent rounded-full animate-spin-reverse ${darkMode ? 'border-red-500' : 'border-red-600'}`}></div>
            </div>
            <div className="text-center">
              <p className={`text-2xl font-bold ${darkMode ? 'text-white' : 'text-slate-800'}`}>Loading Dashboard Data...</p>
              <p className={`text-lg mt-2 ${darkMode ? 'text-slate-400' : 'text-slate-600'}`}>Fetching real-time fatigue monitoring data</p>
            </div>
          </div>
        </div>
      )}

      {/* --- NOTIFICATIONS STACK CONTAINER --- */}
      <div className="absolute top-[10vh] right-[2vw] z-[100] flex flex-col gap-4 pointer-events-none max-w-[600px] w-[30vw]">
        {notifications.map((notif) => (
          <div 
            key={notif.id}
            className={`pointer-events-auto p-6 rounded-xl shadow-2xl border-l-8 flex items-start gap-4 w-full animate-in slide-in-from-right duration-300 ${darkMode ? 'bg-slate-800 border-red-500 text-white' : 'bg-white border-red-500 text-slate-800'}`}
          >
            <div className="p-3 bg-red-500/20 rounded-full text-red-500 animate-pulse shrink-0">
              <Bell size={32} />
            </div>
            <div className="flex-1 min-w-0">
              <h4 className="font-bold text-xl truncate">{notif.title}</h4>
              <p className={`text-lg mt-1 break-words leading-snug ${darkMode ? 'text-slate-200' : 'opacity-80'}`}>{notif.message}</p>
            </div>
            <button 
              type="button"
              onClick={(e) => {
                e.preventDefault();
                e.stopPropagation();
                removeNotification(notif.id);
              }} 
              className="text-slate-500 hover:text-red-500 shrink-0 cursor-pointer pointer-events-auto p-1 rounded-full hover:bg-slate-700/50 transition-colors"
            >
              <X size={28} />
            </button>
          </div>
        ))}
      </div>

      {/* --- MODAL DETAIL ALERT --- */}
      {selectedAlert && (
        <div className="fixed inset-0 z-[60] flex items-center justify-center p-4 bg-black/80 backdrop-blur-md animate-in fade-in duration-200">
          <div className={`w-full max-w-[90vw] h-[85vh] rounded-3xl shadow-2xl overflow-hidden flex flex-col ${darkMode ? 'bg-slate-900 text-white border border-slate-700' : 'bg-slate-100 text-slate-900'}`}>
            {/* Header */}
            <div className="p-[3vh] border-b border-inherit flex justify-between items-start shrink-0">
              <div>
                <div className="flex items-center gap-4 mb-3">
                  <h2 className="text-4xl font-bold flex items-center gap-4">
                    <span className="bg-red-500 text-white p-2 rounded"><AlertTriangle size={36} /></span>
                    FATIGUE ALERT DETAIL
                  </h2>
                  <span className="px-4 py-2 bg-red-100 text-red-600 rounded-full text-xl font-bold border border-red-200 uppercase tracking-wider">
                    {selectedAlert.status}
                  </span>
                </div>
                <p className={`text-2xl ${darkMode ? 'text-slate-300' : 'opacity-70'}`}>
                  Unit <span className="font-mono font-bold bg-slate-700 text-white px-3 py-1 rounded mx-2">{selectedAlert.unitName}</span> 
                  verificated by <strong className="text-red-400">{selectedAlert.operatorName}</strong>
                </p>
              </div>
              <button 
                onClick={() => setSelectedAlert(null)} 
                className="p-3 hover:bg-slate-700/50 rounded-full transition-colors"
              >
                <X size={48} />
              </button>
            </div>
            
            {/* Body */}
            <div className="flex-1 p-[3vh] overflow-hidden">
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-[4vh] h-full">
                {/* LEFT */}
                <div className="flex flex-col gap-[2vh] h-full overflow-hidden">
                  <h3 className={`font-bold text-xl uppercase tracking-wider flex items-center gap-3 shrink-0 ${darkMode ? 'text-slate-200' : 'opacity-70'}`}>
                    <Camera size={28}/> In-Cabin Camera Feed
                  </h3>
                  <div className="flex-1 min-h-0 relative rounded-2xl bg-black border-4 border-slate-700 overflow-hidden group">
                    <div className="absolute inset-0 bg-green-900/10 z-10 pointer-events-none mix-blend-overlay"></div>
                    <div className="absolute inset-0 bg-[radial-gradient(circle,transparent_40%,#000_100%)] z-20 pointer-events-none"></div>
                    {selectedAlert.imageUrl ? (
                      <>
                        <img 
                          src={selectedAlert.imageUrl} 
                          alt="Camera Feed" 
                          className="absolute inset-0 w-full h-full object-contain bg-black"
                        />
                        {/* Red overlay label */}
                        <div className="absolute inset-0 flex items-center justify-center z-30 pointer-events-none">
                          <div className="border-4 border-red-500/80 rounded-xl shadow-[0_0_20px_rgba(239,68,68,0.5)] animate-pulse flex flex-col items-center justify-end p-4 min-w-[200px]">
                            <div className="bg-red-600 text-white text-lg font-bold px-4 py-2 rounded-lg flex items-center gap-2 shadow-lg">
                              <EyeOff size={20} /> {selectedAlert.alertType || 'FATIGUE'}
                            </div>
                          </div>
                        </div>
                      </>
                    ) : (
                      <div className="absolute inset-0 flex items-center justify-center">
                        <div className="border-4 border-red-500/80 rounded-xl shadow-[0_0_20px_rgba(239,68,68,0.5)] animate-pulse flex flex-col items-center justify-center p-8">
                          <EyeOff size={64} className="text-red-500 mb-4" />
                          <div className="bg-red-600 text-white text-xl font-bold px-6 py-3 rounded-lg flex items-center gap-3 shadow-lg">
                            {selectedAlert.alertType || 'No Image'}
                          </div>
                        </div>
                      </div>
                    )}
                  </div>
                  <div className="grid grid-cols-2 gap-4 shrink-0">
                    <div className={`p-4 rounded-xl border flex flex-col justify-center ${darkMode ? 'bg-slate-800 border-slate-700' : 'bg-white border-slate-300'}`}>
                      <span className={`text-sm block uppercase tracking-wider font-bold mb-1 ${darkMode ? 'text-slate-300' : 'opacity-60'}`}>Vehicle Speed</span>
                      <span className="font-mono font-bold text-4xl">{selectedAlert.speed || 0} km/h</span>
                    </div>
                    <div className={`p-4 rounded-xl border flex flex-col justify-center ${darkMode ? 'bg-slate-800 border-slate-700' : 'bg-white border-slate-300'}`}>
                      <span className={`text-sm block uppercase tracking-wider font-bold mb-1 ${darkMode ? 'text-slate-300' : 'opacity-60'}`}>Fatigue Type</span>
                      <span className="font-bold text-3xl text-red-500">{selectedAlert.alertType || 'Unknown'}</span>
                    </div>
                  </div>
                </div>
                {/* RIGHT */}
                <div className="flex flex-col gap-[2vh] h-full">
                  <h3 className={`font-bold text-xl uppercase tracking-wider flex items-center gap-3 shrink-0 ${darkMode ? 'text-slate-200' : 'opacity-70'}`}>
                    <Map size={28}/> Event Location
                  </h3>
                  <div className={`flex-1 relative rounded-2xl border border-slate-600 overflow-hidden ${darkMode ? 'bg-slate-800' : 'bg-slate-300'}`}>
                    <div className="absolute inset-0 opacity-20" style={{backgroundImage: `linear-gradient(${darkMode ? '#fff' : '#000'} 1px, transparent 1px), linear-gradient(90deg, ${darkMode ? '#fff' : '#000'} 1px, transparent 1px)`, backgroundSize: '60px 60px'}}></div>
                    <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 flex flex-col items-center group cursor-pointer">
                      <div className="w-32 h-32 rounded-full bg-red-500/20 animate-ping absolute top-0"></div>
                      <div className="relative z-10 text-red-600 drop-shadow-xl transform group-hover:-translate-y-2 transition-transform"><MapPin size={96} fill={darkMode ? "#ef4444" : "#dc2626"} className="text-white" /></div>
                      <div className="mt-4 bg-slate-900 text-white text-xl px-6 py-3 rounded-xl shadow-lg font-bold whitespace-nowrap z-20">{selectedAlert.location}</div>
                    </div>
                  </div>
                  <div className={`p-6 rounded-xl border shrink-0 flex items-center justify-between ${darkMode ? 'bg-slate-800 border-slate-700' : 'bg-white border-slate-300'}`}>
                    <div>
                      <span className={`text-sm block font-bold ${darkMode ? 'text-slate-300' : 'opacity-60'}`}>GPS Coordinates</span>
                      <span className="font-mono text-2xl font-medium text-blue-500">Lat: {selectedAlert.latitude?.toFixed(4)}, Long: {selectedAlert.longitude?.toFixed(4)}</span>
                      <div className={`text-sm mt-2 ${darkMode ? 'text-slate-400' : 'opacity-50'}`}>Area: {selectedAlert.area}</div>
                    </div>
                    <button className="flex items-center gap-3 px-8 py-4 rounded-xl bg-blue-600 text-white hover:bg-blue-700 font-bold text-xl transition-colors">
                      <Radio size={24} /> Contact Unit
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* --- HEADER --- */}
      <header className={`h-[8vh] border-b flex items-center justify-between px-[2vw] shrink-0 ${darkMode ? 'border-slate-700 bg-slate-900' : 'border-slate-300 bg-slate-100'}`}>
        <div className="flex items-center gap-6">
          <div className="hidden lg:block border-r pr-8 mr-2 border-inherit">
            <img 
              src="/alamtri-logo.svg" 
              alt="Alamtri Logo" 
              className="h-[4vh] object-contain"
            />
          </div>
          <div className="p-3 bg-red-600 rounded-xl shadow-lg shadow-red-500/30">
            <ShieldAlert className="w-[3vh] h-[3vh] text-white" />
          </div>
          <div>
            <h1 className="text-[clamp(1.5rem,2vh,2.5rem)] font-black tracking-tight leading-tight">FATIGUE COMMAND CENTER</h1>
            <p className={`text-[clamp(1rem,1.2vh,1.5rem)] font-medium ${darkMode ? 'text-slate-200' : 'text-slate-500'}`}>Operational Monitoring Dashboard</p>
          </div>
        </div>

        <div className="flex items-center gap-[2vw]">
          {/* SSE Connection Status Indicator */}
          <SseStatusIndicator 
            status={sseStatus}
            lastHeartbeat={lastHeartbeat}
            onReconnect={sseReconnect}
            darkMode={darkMode}
            showText={true}
          />

          <div className={`hidden md:flex items-center gap-6 px-[1.5vw] py-[1vh] rounded-full border ${darkMode ? 'bg-slate-800 border-slate-700' : 'bg-white border-slate-300 shadow-sm'}`}>
            <div className="flex items-center gap-3">
              <div className={`w-3 h-3 lg:w-4 lg:h-4 rounded-full ${sensorHealth.coverage >= 80 ? 'bg-emerald-500' : sensorHealth.coverage >= 50 ? 'bg-amber-500 animate-pulse' : 'bg-red-500 animate-pulse'}`}></div>
              <span className={`text-[clamp(1rem,1.2vh,1.5rem)] font-semibold whitespace-nowrap ${darkMode ? 'text-slate-200' : 'text-slate-700'}`}>Sensor Health: {sensorHealth.coverage}%</span>
            </div>
            <div className="h-6 w-px bg-slate-600/30"></div>
            <div className="flex items-center gap-4 text-[clamp(1rem,1.2vh,1.5rem)]">
              <div className="flex items-center gap-2" title="Total Followed Up"><Wifi className="w-[2.5vh] h-[2.5vh] text-emerald-500" /> <span className={`font-mono font-bold ${darkMode ? 'text-white' : ''}`}>{sensorHealth.followedUp}</span></div>
              <div className="flex items-center gap-2" title="Total Waiting Follow Up"><WifiOff className="w-[2.5vh] h-[2.5vh] text-red-500 ml-1" /> <span className={`font-mono font-bold ${darkMode ? 'text-white' : ''}`}>{sensorHealth.waitingFollowUp}</span></div>
            </div>
          </div>

          <div className={`flex flex-col items-end ${darkMode ? 'text-white' : 'text-slate-600'}`}>
            <span className="text-[clamp(1.8rem,3vh,3.5rem)] font-mono font-bold leading-none">
              {currentTime.toLocaleTimeString('id-ID', { hour: '2-digit', minute: '2-digit', second: '2-digit' })} WITA
            </span>
            <span className={`text-[clamp(0.9rem,1.2vh,1.3rem)] font-medium uppercase tracking-wider mt-1 ${darkMode ? 'text-slate-300' : ''}`}>
              {currentTime.toLocaleDateString('id-ID', { weekday: 'long', day: 'numeric', month: 'short', year: 'numeric' })}
            </span>
          </div>
          
          <button 
            onClick={() => setDarkMode(!darkMode)}
            className={`p-[1vh] rounded-full transition-colors ${darkMode ? 'bg-slate-800 hover:bg-slate-700 text-yellow-400' : 'bg-slate-300 hover:bg-slate-400 text-slate-700'}`}
          >
            {darkMode ? <Sun className="w-[3.5vh] h-[3.5vh]" /> : <Moon className="w-[3.5vh] h-[3.5vh]" />}
          </button>
        </div>
      </header>

      {/* --- MAIN CONTENT --- */}
      <main className="flex-1 flex p-[2vh] gap-[2vh] overflow-hidden min-h-0">
        
        {/* --- LEFT PANEL & MIDDLE --- */}
        <div className="flex-[4] flex flex-col gap-[2vh] min-w-0 h-full">
            
          {/* 1. KPI CARDS */}
          <div className="flex gap-[2vh] shrink-0 h-[16vh] min-h-[140px]">
            
            <div className="flex-[1.8] flex gap-[2vh]">
              <div className={`flex-1 p-[2vh] rounded-2xl border flex items-center justify-between ${getCardBg()}`}>
                <div className="flex flex-col justify-center h-full">
                  <p className={`text-[clamp(1rem,1.4vh,1.6rem)] uppercase font-bold tracking-wider mb-2 ${darkMode ? 'text-slate-200' : 'text-slate-500'}`}>
                    {selectedArea === 'All' ? 'TOTAL' : selectedArea.toUpperCase()} ALARMS
                  </p>
                  <h2 className={`text-[clamp(3rem,5vh,6rem)] font-black leading-tight ${darkMode ? 'text-white' : 'text-slate-900'}`}>{stats.totalToday}</h2>
                </div>
                <div className={`p-[1.5vh] rounded-full ${darkMode ? 'bg-blue-500/20 text-blue-400' : 'bg-blue-100 text-blue-600'}`}>
                  <Radio className="w-[4vh] h-[4vh]" />
                </div>
              </div>

              <div className={`flex-1 p-[2vh] rounded-2xl border flex items-center justify-between ${getCardBg()}`}>
                <div className="flex flex-col justify-center h-full">
                  <p className={`text-[clamp(1rem,1.4vh,1.6rem)] uppercase font-bold tracking-wider mb-2 ${darkMode ? 'text-slate-200' : 'text-slate-500'}`}>
                    {getAreaTitle()}FOLLOWED UP
                  </p>
                  <h2 className="text-[clamp(3rem,5vh,6rem)] font-black leading-tight text-emerald-500">{stats.followedUpToday}</h2>
                </div>
                <div className={`p-[1.5vh] rounded-full ${darkMode ? 'bg-emerald-500/20 text-emerald-400' : 'bg-emerald-100 text-emerald-600'}`}>
                  <CheckCircle className="w-[4vh] h-[4vh]" />
                </div>
              </div>
            </div>

            <div className={`flex-1 p-[2vh] rounded-2xl border flex items-center justify-between relative overflow-hidden ${darkMode ? 'bg-red-900/20 border-red-500/50' : 'bg-red-50 border-red-200'}`}>
              <div className="z-10 flex flex-col justify-center h-full">
                <p className="text-[clamp(1rem,1.4vh,1.6rem)] uppercase font-bold tracking-wider text-red-500 mb-2">
                  {getAreaTitle()}WAITING FOLLOW UP
                </p>
                <h2 className="text-[clamp(3.5rem,6vh,7rem)] font-black leading-tight text-red-600">{stats.activeOpen}</h2>
              </div>
              <div className="p-[1.5vh] rounded-full bg-red-500 text-white animate-pulse z-10">
                <AlertTriangle className="w-[5vh] h-[5vh]" />
              </div>
              <div className="absolute -right-4 -bottom-4 w-[15vh] h-[15vh] bg-red-500/10 rounded-full blur-xl"></div>
            </div>
          </div>

          {/* 2. MIDDLE AREA */}
          <div className="flex-[1.5] flex gap-[2vh] min-h-0">
            
            {/* AREA DISTRIBUTION */}
            <div className={`flex-[1.8] rounded-2xl border flex flex-col p-[2vh] overflow-hidden ${getCardBg()}`}>
              <div className="flex items-center mb-[1.5vh] shrink-0">
                <div className={`px-4 py-2 rounded-lg text-[clamp(1rem,1.5vh,1.8rem)] font-bold flex items-center gap-3 ${darkMode ? 'bg-slate-900/80 text-white' : 'bg-white/90 text-slate-800 shadow-sm border border-slate-200'}`}>
                  <LayoutGrid size={24} /> 
                  {selectedArea === 'All' ? 'AREA' : selectedArea.toUpperCase()} DISTRIBUTION
                  <span className="w-3 h-3 rounded-full bg-blue-500 animate-pulse ml-2"></span>
                </div>
              </div>

              <div className="flex-1 overflow-hidden flex flex-col">
                <div className={`grid gap-[2vh] h-full ${selectedArea === 'All' ? 'grid-cols-2' : 'grid-cols-1'}`}>
                  
                  {(selectedArea === 'All' || selectedArea === 'Mining') && (
                    <div className={`rounded-xl p-[1.5vh] flex flex-col h-full ${darkMode ? 'bg-slate-900/40' : 'bg-white/60 border border-slate-200'}`}>
                      <div className={`flex items-center justify-between mb-2 border-b pb-2 shrink-0 ${darkMode ? 'border-slate-700' : 'border-slate-200'}`}>
                        <h3 className={`text-[clamp(1rem,1.5vh,1.8rem)] font-bold uppercase flex items-center gap-2 ${darkMode ? 'text-blue-400' : 'text-blue-600'}`}>
                          <Activity size={20} /> Mining
                        </h3>
                        <div className="flex items-center gap-3 text-[clamp(0.8rem,1vh,1.2rem)]">
                          <span className={darkMode ? 'text-slate-200 font-bold' : 'text-slate-600'}>{areaSummary.Mining.total} Total</span>
                          <span className="text-red-500 font-bold">{areaSummary.Mining.open} Open</span>
                        </div>
                      </div>
                      <div className="flex-1 flex flex-col justify-between overflow-hidden" ref={miningListContainerRef}>
                        <div className="grid grid-cols-2 gap-3 mt-1">
                          {Object.keys(locationStats.Mining).length > 0 ? (
                            paginate(Object.entries(locationStats.Mining), miningPage, dynamicItemsPerPage.mining).map(([location, count]) => (
                              <div key={location} onClick={() => { setSelectedArea('Mining'); setSelectedLocationFilter(location); }} className={`p-3 rounded-xl border flex flex-col justify-between items-center text-center transition-all cursor-pointer hover:scale-105 ${selectedLocationFilter === location ? (darkMode ? 'bg-blue-900/40 border-blue-400 ring-1 ring-blue-500' : 'bg-blue-50 border-blue-500 ring-1 ring-blue-200') : (darkMode ? 'bg-slate-800 border-slate-600 hover:border-slate-500' : 'bg-white border-slate-200 shadow-sm hover:border-blue-300')}`}>
                                <span className={`text-[clamp(0.9rem,1.2vh,1.4rem)] font-medium mb-1 line-clamp-1 ${darkMode ? 'text-white' : 'text-slate-600'}`}>{location}</span>
                                <span className="text-[clamp(1.8rem,2.5vh,3rem)] font-black text-red-500 leading-none">{count}</span>
                              </div>
                            ))
                          ) : <div className="col-span-2 py-4 text-center text-slate-500 text-lg italic">No active alerts</div>}
                        </div>
                        <PaginationControls currentPage={miningPage} totalPages={Math.ceil(Object.keys(locationStats.Mining).length / dynamicItemsPerPage.mining)} onPageChange={setMiningPage} />
                      </div>
                    </div>
                  )}

                  {(selectedArea === 'All' || selectedArea === 'Hauling') && (
                    <div className={`rounded-xl p-[1.5vh] flex flex-col h-full ${darkMode ? 'bg-slate-900/40' : 'bg-white/60 border border-slate-200'}`}>
                      <div className={`flex items-center justify-between mb-2 border-b pb-2 shrink-0 ${darkMode ? 'border-slate-700' : 'border-slate-200'}`}>
                        <h3 className={`text-[clamp(1rem,1.5vh,1.8rem)] font-bold uppercase flex items-center gap-2 ${darkMode ? 'text-teal-400' : 'text-teal-600'}`}>
                          <Truck size={20} /> Hauling
                        </h3>
                        <div className="flex items-center gap-3 text-[clamp(0.8rem,1vh,1.2rem)]">
                          <span className={darkMode ? 'text-slate-200 font-bold' : 'text-slate-600'}>{areaSummary.Hauling.total} Total</span>
                          <span className="text-red-500 font-bold">{areaSummary.Hauling.open} Open</span>
                        </div>
                      </div>
                      <div className="flex-1 flex flex-col justify-between overflow-hidden" ref={haulingListContainerRef}>
                        <div className="grid grid-cols-2 gap-3 mt-1">
                          {Object.keys(locationStats.Hauling).length > 0 ? (
                            paginate(Object.entries(locationStats.Hauling), haulingPage, dynamicItemsPerPage.hauling).map(([location, count]) => (
                              <div key={location} onClick={() => { setSelectedArea('Hauling'); setSelectedLocationFilter(location); }} className={`p-3 rounded-xl border flex flex-col justify-between items-center text-center transition-all cursor-pointer hover:scale-105 ${selectedLocationFilter === location ? (darkMode ? 'bg-teal-900/40 border-teal-400 ring-1 ring-teal-500' : 'bg-teal-50 border-teal-500 ring-1 ring-teal-200') : (darkMode ? 'bg-slate-800 border-slate-600 hover:border-slate-500' : 'bg-white border-slate-200 shadow-sm hover:border-teal-300')}`}>
                                <span className={`text-[clamp(0.9rem,1.2vh,1.4rem)] font-medium mb-1 line-clamp-1 ${darkMode ? 'text-white' : 'text-slate-600'}`}>{location}</span>
                                <span className="text-[clamp(1.8rem,2.5vh,3rem)] font-black text-red-500 leading-none">{count}</span>
                              </div>
                            ))
                          ) : <div className="col-span-2 py-4 text-center text-slate-500 text-lg italic">No active alerts</div>}
                        </div>
                        <PaginationControls currentPage={haulingPage} totalPages={Math.ceil(Object.keys(locationStats.Hauling).length / dynamicItemsPerPage.hauling)} onPageChange={setHaulingPage} />
                      </div>
                    </div>
                  )}
                </div>
              </div>
            </div>

            {/* DELAYED FOLLOW UP */}
            <div className={`flex-1 rounded-2xl border-2 flex flex-col p-[2vh] overflow-hidden ${darkMode ? 'bg-red-900/10 border-red-500 shadow-[0_0_20px_rgba(239,68,68,0.3)]' : 'bg-red-50 border-red-600 shadow-md'}`}>
              <div className="flex justify-between items-center mb-[1.5vh] shrink-0 border-b pb-3 border-inherit">
                <div className="flex items-center gap-3">
                  <h3 className="text-[clamp(1rem,1.5vh,1.8rem)] font-bold uppercase flex items-center gap-2 text-red-500 animate-pulse">
                    <Timer size={24} /> DELAYED FOLLOW UP
                  </h3>
                  <span className="bg-red-500 text-white text-[1rem] px-3 py-1 rounded-full font-bold">{'>'} 30m</span>
                </div>
                <span className={`text-[1.2rem] font-bold ${darkMode ? 'text-white' : 'text-slate-600'}`}>
                  <span className="text-red-500 text-2xl mr-2">{overdueAlerts.length}</span>Total
                </span>
              </div>
              <div className="flex-1 flex flex-col justify-between overflow-hidden">
                <div className="flex-1 space-y-2 overflow-hidden">
                  {overdueAlerts.length > 0 ? (
                    paginate(overdueAlerts, delayedPage, ITEMS_DELAYED).map((alert) => (
                      <div key={alert.id} onClick={() => setSelectedAlert({...alert, status: 'Delayed'})} className={`relative group p-3 rounded-xl border flex justify-between items-center cursor-pointer transition-all hover:bg-red-500/10 hover:border-red-400 ${darkMode ? 'bg-slate-900 border-red-500/30' : 'bg-white border-red-200 shadow-sm'}`}>
                        <div>
                          <div className={`font-bold text-[clamp(1rem,1.2vh,1.4rem)] ${darkMode ? 'text-white' : 'text-slate-900'}`}>{alert.unitName}</div>
                          <div className={`text-[1rem] ${darkMode ? 'text-slate-300' : 'text-slate-500'}`}>{alert.location}</div>
                        </div>
                        <div className="text-right">
                          <div className="text-[1.2rem] font-black text-red-500 font-mono">+{alert.dynamicDelayMinutes}m</div>
                          <div className="text-[0.9rem] text-red-400 uppercase font-bold">LATE</div>
                        </div>
                      </div>
                    ))
                  ) : <div className="h-full flex flex-col items-center justify-center text-emerald-500 opacity-70"><CheckCircle size={48} className="mb-2" /><span className="text-xl text-center">No overdue alerts.<br/>Great Job!</span></div>}
                </div>
                <PaginationControls currentPage={delayedPage} totalPages={Math.ceil(overdueAlerts.length / ITEMS_DELAYED)} onPageChange={setDelayedPage} />
              </div>
            </div>
          </div>

          {/* 3. STRATEGIC INSIGHTS */}
          <div className="flex-[1.2] flex gap-[2vh] min-h-0">
            {/* Recurrent Fatigue Units */}
            <div className={`flex-1 rounded-2xl border p-[2vh] overflow-hidden flex flex-col ${getCardBg()}`}>
              <div className="flex items-center justify-between mb-[1.5vh] shrink-0">
                <div className="flex items-center gap-3">
                  <Users className="w-[3vh] h-[3vh] text-red-500" />
                  <h3 className={`text-[clamp(1rem,1.5vh,1.8rem)] font-bold uppercase tracking-wider ${darkMode ? 'text-white' : 'text-slate-600'}`}>
                    RECURRENT UNITS
                  </h3>
                </div>
                <span className="bg-red-600 text-white text-[1rem] font-bold px-3 py-1 rounded-full">{highRiskOperators.length} Total</span>
              </div>
              <div className="flex-1 flex flex-col min-h-0 overflow-hidden" ref={recurrentListContainerRef}>
                <div className="flex-1 space-y-2 overflow-y-auto">
                  {highRiskOperators.length > 0 ? (
                    paginate(highRiskOperators, recurrentPage, dynamicItemsPerPage.recurrent).map((op) => (
                      <div key={op.id} className={`px-3 py-2 rounded-xl border-l-4 border-red-500 flex justify-between items-center ${darkMode ? 'bg-slate-900/50' : 'bg-white border border-slate-200'}`}>
                        <div>
                          <div className={`font-bold text-[clamp(1rem,1.2vh,1.4rem)] ${darkMode ? 'text-white' : 'text-slate-800'}`}>{op.unitName}</div>
                          <div className={`text-[0.9rem] ${darkMode ? 'text-slate-300' : 'text-slate-500'}`}>{op.operatorName} • <span className="text-red-500 font-semibold">{op.eventCount} events</span></div>
                        </div>
                        <div className="text-[0.9rem] text-red-500 font-bold px-3 py-1 bg-red-500/10 rounded">{op.status?.toUpperCase() || 'MONITORING'}</div>
                      </div>
                    ))
                  ) : <div className="flex h-full items-center justify-center text-slate-500 text-lg">No recurrent data</div>}
                </div>
                <div className="shrink-0">
                  <PaginationControls currentPage={recurrentPage} totalPages={Math.ceil(highRiskOperators.length / dynamicItemsPerPage.recurrent)} onPageChange={setRecurrentPage} />
                </div>
              </div>
            </div>

            {/* High Risk Fatigue Area */}
            <div className={`flex-1 rounded-2xl border p-[2vh] overflow-hidden flex flex-col ${getCardBg()}`}>
              <div className="flex items-center justify-between mb-[1.5vh] shrink-0">
                <div className="flex items-center gap-3">
                  <MapPin className="w-[3vh] h-[3vh] text-orange-500" />
                  <h3 className={`text-[clamp(1rem,1.5vh,1.8rem)] font-bold uppercase tracking-wider ${darkMode ? 'text-white' : 'text-slate-600'}`}>
                    HIGH RISK AREA
                  </h3>
                </div>
                <span className="bg-orange-600 text-white text-[1rem] font-bold px-3 py-1 rounded-full">{highFreqZones.length} Total</span>
              </div>
              <div className="flex-1 flex flex-col min-h-0 overflow-hidden" ref={highRiskListContainerRef}>
                <div className="flex-1 space-y-2 overflow-y-auto">
                  {highFreqZones.length > 0 ? (
                    paginate(highFreqZones, highRiskPage, dynamicItemsPerPage.highRisk).map((zone) => (
                      <div key={zone.id} className={`px-3 py-2 rounded-xl border flex justify-between items-center ${darkMode ? 'bg-slate-900/50 border-slate-700' : 'bg-white border-slate-200'}`}>
                        <div>
                          <div className="flex items-center gap-2">
                            <span className="w-3 h-3 rounded-full bg-red-500"></span>
                            <span className={`text-[clamp(1rem,1.2vh,1.4rem)] font-bold ${darkMode ? 'text-white' : 'text-slate-800'}`}>{zone.location}</span>
                          </div>
                          <div className={`text-[0.9rem] ml-5 ${darkMode ? 'text-slate-300' : 'text-slate-500'}`}>Events: <span className="font-mono font-bold text-slate-400">{zone.eventCount}</span></div>
                        </div>
                        <div className={`text-[0.9rem] px-3 py-1 rounded border flex items-center gap-2 font-medium ${zone.area === 'Mining' ? 'bg-blue-500/10 text-blue-500 border-blue-500/20' : 'bg-teal-500/10 text-teal-500 border-teal-500/20'}`}>
                          {zone.area === 'Mining' ? <Activity size={16} /> : <Truck size={16} />} {zone.area?.toUpperCase()}
                        </div>
                      </div>
                    ))
                  ) : <div className="flex h-full items-center justify-center text-slate-500 text-lg">No high risk data</div>}
                </div>
                <div className="shrink-0">
                  <PaginationControls currentPage={highRiskPage} totalPages={Math.ceil(highFreqZones.length / dynamicItemsPerPage.highRisk)} onPageChange={setHighRiskPage} />
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* --- RIGHT PANEL: GLOBAL FILTER & ACTIVE FATIGUE LIST --- */}
        <div className="flex-1 flex flex-col gap-[2vh] min-w-[320px] lg:min-w-[400px] h-full">
            
          {/* 1. GLOBAL FILTER CARD */}
          <div className={`p-[2vh] rounded-2xl border flex flex-col justify-center shrink-0 h-[16vh] min-h-[140px] ${getCardBg()}`}>
            <div className="flex items-center gap-3 mb-3">
              <Filter size={24} className={darkMode ? 'text-white' : 'text-slate-500'} />
              <span className={`text-[clamp(1rem,1.2vh,1.5rem)] font-bold uppercase tracking-wider ${darkMode ? 'text-white' : 'text-slate-500'}`}>Global Filter</span>
            </div>
            <div className={`flex p-2 rounded-xl ${darkMode ? 'bg-slate-900' : 'bg-slate-200'}`}>
              {['All', 'Mining', 'Hauling'].map((area) => (
                <button key={area} onClick={() => handleAreaTabClick(area)} className={`flex-1 py-[1.5vh] text-[clamp(1rem,1.5vh,1.6rem)] font-bold rounded-lg transition-all ${selectedArea === area ? (darkMode ? 'bg-blue-600 text-white shadow' : 'bg-white text-slate-900 shadow') : 'text-slate-500 hover:text-slate-400'}`}>{area}</button>
              ))}
            </div>
            <div className={`mt-3 text-[1rem] text-right ${darkMode ? 'text-slate-200' : 'opacity-60'}`}>
              Viewing: <span className="font-bold text-blue-500">{selectedArea.toUpperCase()}</span>
              {selectedLocationFilter && <span> • <span className="text-amber-500">{selectedLocationFilter}</span></span>}
            </div>
          </div>

          {/* 2. ACTIVE FATIGUE RECENT LIST */}
          <div className={`flex-1 flex flex-col rounded-2xl border overflow-hidden ${getCardBg()}`}>
            <div className="p-[2.5vh] border-b border-inherit shrink-0">
              <div className="flex items-center justify-between mb-2">
                <div className="flex items-center gap-3">
                  <Zap className="text-yellow-500 w-[3.5vh] h-[3.5vh]" />
                  <h3 className={`font-bold text-[clamp(1.5rem,2vh,2.5rem)] ${darkMode ? 'text-white' : 'text-slate-800'}`}>Active Alerts</h3>
                </div>
                <span className={`text-lg font-bold ${darkMode ? 'text-white' : 'text-slate-600'}`}>{filteredAlerts.length} Total</span>
              </div>
              <div className={`flex justify-between items-center text-[1rem] ${darkMode ? 'text-slate-300' : 'opacity-60'}`}><span>Recent Alerts</span><span className="text-red-500 font-bold">OPEN ≤ 30m</span></div>
            </div>

            <div className="flex-1 flex flex-col justify-between overflow-hidden p-[2vh]" ref={activeFatigueListContainerRef}>
              <div className="space-y-[1.5vh]">
                {filteredAlerts.length === 0 ? (
                  <div className="h-full flex flex-col items-center justify-center text-slate-500 opacity-60">
                    <CheckCircle size={64} className="mb-4" /><p className="text-xl text-center">All clear</p>
                    {selectedLocationFilter && <button onClick={() => setSelectedLocationFilter(null)} className="mt-4 text-blue-500 underline text-lg">Clear Filter</button>}
                  </div>
                ) : (
                  paginate(filteredAlerts, activeFatiguePage, dynamicItemsPerPage.activeFatigue).map((alert) => (
                    <div key={alert.id} className={`relative p-[1.5vh] rounded-xl border transition-all hover:scale-[1.01] cursor-pointer group ${darkMode ? 'bg-slate-800 border-l-8 border-l-red-500 border-y-slate-700 border-r-slate-700 hover:border-slate-500' : 'bg-white border-l-8 border-l-red-500 border-y-slate-200 border-r-slate-200 shadow-sm hover:shadow-md'}`} onClick={() => setSelectedAlert(alert)}>
                      <div className="flex justify-between items-start mb-2">
                        <div className="flex items-center gap-3">
                          <span className={`px-2 py-1 rounded-md text-[0.9rem] font-bold uppercase bg-red-600 text-white`}>{alert.alertType}</span>
                          <span className={`text-[1rem] font-mono ${darkMode ? 'text-slate-300' : 'text-slate-500'}`}>{alert.eventTimeFormatted}</span>
                        </div>
                        <span className="text-[0.9rem] text-red-500 font-bold flex items-center gap-2 animate-pulse"><AlertTriangle size={14} /> ACTIVE</span>
                      </div>
                      <div className="flex items-center gap-3 mb-2">
                        <div className={`p-2 rounded-xl ${darkMode ? 'bg-slate-700' : 'bg-slate-100'}`}><Truck className={`w-[3vh] h-[3vh] ${darkMode ? 'text-slate-300' : 'text-slate-600'}`} /></div>
                        <div>
                          <h4 className={`font-bold text-[clamp(1.2rem,1.5vh,1.8rem)] ${darkMode ? 'text-slate-200' : 'text-slate-800'}`}>{alert.unitName}</h4>
                          <p className={`text-[1rem] ${darkMode ? 'text-slate-300' : 'text-slate-500'}`}>{alert.operatorName} • {alert.alertCountToday}x Today</p>
                        </div>
                      </div>
                      <div className={`text-[1rem] p-2 rounded-lg flex justify-between items-center ${darkMode ? 'bg-slate-900' : 'bg-slate-100'}`}>
                        <div className={`flex items-center gap-2 font-medium ${darkMode ? 'text-slate-300' : 'text-slate-500'}`}><Map size={18} /><span>{alert.location}</span></div>
                        <div className="flex items-center gap-2 text-red-400 font-mono font-bold animate-pulse"><Clock size={18} /><span>+{alert.dynamicDurationMinutes}m</span></div>
                      </div>
                    </div>
                  ))
                )}
              </div>
              <PaginationControls currentPage={activeFatiguePage} totalPages={Math.ceil(filteredAlerts.length / dynamicItemsPerPage.activeFatigue)} onPageChange={setActiveFatiguePage} />
            </div>
          </div>
        </div>
      </main>
    </div>
  );
};

export default SCCDashboard;
