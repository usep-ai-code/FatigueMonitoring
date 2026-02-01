import React, { useState, useEffect, useMemo, useRef } from 'react';
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
  ChevronRight
} from 'lucide-react';

const emptyDashboard = {
  generatedAtUtc: null,
  deviceHealth: {
    totalDevices: 0,
    onlineDevices: 0,
    offlineDevices: 0,
    coveragePercent: 0
  },
  areaKpis: [],
  areaDistribution: [],
  activeAlerts: [],
  delayedAlerts: [],
  recurrentUnits: [],
  highRiskAreas: []
};

const SCCDashboard = () => {
  const [darkMode, setDarkMode] = useState(true);
  const [currentTime, setCurrentTime] = useState(new Date());
  const [selectedArea, setSelectedArea] = useState('All'); 
  const [sseStatus, setSseStatus] = useState('Connecting');
  const [dashboardData, setDashboardData] = useState(emptyDashboard);
  
  const [notifications, setNotifications] = useState([]);
  const [selectedAlert, setSelectedAlert] = useState(null);
  const [selectedLocationFilter, setSelectedLocationFilter] = useState(null);

  // --- REFS UNTUK MENGUKUR TINGGI CONTAINER (DYNAMIC PAGINATION) ---
  const miningListContainerRef = useRef(null);
  const haulingListContainerRef = useRef(null);
  const activeFatigueListContainerRef = useRef(null);
  const recurrentListContainerRef = useRef(null);
  const highRiskListContainerRef = useRef(null); 
  const knownAlertIdsRef = useRef(new Set());
  const hasInitializedAlertsRef = useRef(false);

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
    recurrent: 3,
    highRisk: 3
  });

  // --- CONSTANTS FOR OTHER LISTS ---
  const ITEMS_DELAYED = 4; // Dikurangi agar muat font besar

  // --- EFFECT: Calculate Items Per Page based on container height ---
  // UPDATED: Adjusted heights for better fit in bottom row
  useEffect(() => {
    const calculateCapacity = () => {
      // Konstanta estimasi tinggi (Item + Gap)
      const ITEM_HEIGHT_MINING_HAULING = 85; 
      const ITEM_HEIGHT_ACTIVE_FATIGUE = 130; 
      // UPDATED: Dikurangi sedikit agar muat lebih banyak (karena padding item dikurangi)
      const ITEM_HEIGHT_RECURRENT = 60;      // px (sebelumnya 70)
      const ITEM_HEIGHT_HIGH_RISK = 60;      // px (sebelumnya 70)
      const PAGINATION_HEIGHT_BUFFER = 35;   // px (sebelumnya 40)

      // 1. Hitung Kapasitas Mining
      if (miningListContainerRef.current) {
        const height = miningListContainerRef.current.clientHeight;
        const availableHeight = height - PAGINATION_HEIGHT_BUFFER;
        const rows = Math.max(1, Math.floor(availableHeight / ITEM_HEIGHT_MINING_HAULING));
        setDynamicItemsPerPage(prev => ({ ...prev, mining: rows * 2 }));
      }

      // 2. Hitung Kapasitas Hauling
      if (haulingListContainerRef.current) {
        const height = haulingListContainerRef.current.clientHeight;
        const availableHeight = height - PAGINATION_HEIGHT_BUFFER;
        const rows = Math.max(1, Math.floor(availableHeight / ITEM_HEIGHT_MINING_HAULING));
        setDynamicItemsPerPage(prev => ({ ...prev, hauling: rows * 2 }));
      }

      // 3. Hitung Kapasitas Active Fatigue
      if (activeFatigueListContainerRef.current) {
        const height = activeFatigueListContainerRef.current.clientHeight;
        const availableHeight = height - PAGINATION_HEIGHT_BUFFER;
        const rows = Math.max(1, Math.floor(availableHeight / ITEM_HEIGHT_ACTIVE_FATIGUE));
        setDynamicItemsPerPage(prev => ({ ...prev, activeFatigue: rows }));
      }

      // 4. Hitung Kapasitas Recurrent
      if (recurrentListContainerRef.current) {
        const height = recurrentListContainerRef.current.clientHeight;
        const availableHeight = height - PAGINATION_HEIGHT_BUFFER;
        // Force minimum 2 rows if space is tight but technically plausible
        const rows = Math.max(2, Math.floor(availableHeight / ITEM_HEIGHT_RECURRENT));
        setDynamicItemsPerPage(prev => ({ ...prev, recurrent: rows }));
      }

      // 5. Hitung Kapasitas High Risk
      if (highRiskListContainerRef.current) {
        const height = highRiskListContainerRef.current.clientHeight;
        const availableHeight = height - PAGINATION_HEIGHT_BUFFER;
        const rows = Math.max(2, Math.floor(availableHeight / ITEM_HEIGHT_HIGH_RISK));
        setDynamicItemsPerPage(prev => ({ ...prev, highRisk: rows }));
      }
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

  const addNotification = (title, message, type = 'critical') => {
    const id = Date.now() + Math.random();
    const newNotif = { id, title, message, type };
    setNotifications(prev => [newNotif, ...prev]);
    setTimeout(() => {
        setNotifications(prev => prev.filter(n => n.id !== id));
    }, 8000); 
  };

  const removeNotification = (id) => {
    setNotifications(prev => prev.filter(n => n.id !== id));
  };
  const normalizeDashboard = (payload) => ({
    ...emptyDashboard,
    ...payload,
    deviceHealth: {
      ...emptyDashboard.deviceHealth,
      ...(payload?.deviceHealth ?? {})
    },
    areaKpis: payload?.areaKpis ?? [],
    areaDistribution: payload?.areaDistribution ?? [],
    activeAlerts: payload?.activeAlerts ?? [],
    delayedAlerts: payload?.delayedAlerts ?? [],
    recurrentUnits: payload?.recurrentUnits ?? [],
    highRiskAreas: payload?.highRiskAreas ?? []
  });

  useEffect(() => {
    let eventSource;
    let reconnectTimer;

    const connect = () => {
      setSseStatus('Connecting');
      const baseUrl = import.meta.env.VITE_API_BASE_URL ?? '';
      const streamUrl = `${baseUrl.replace(/\/$/, '')}/api/dashboard/stream`;

      eventSource = new EventSource(streamUrl);
      eventSource.onopen = () => setSseStatus('Connected');
      eventSource.onerror = () => {
        setSseStatus('Disconnected');
        eventSource.close();
        reconnectTimer = setTimeout(connect, 3000);
      };
      eventSource.onmessage = (event) => {
        if (!event.data) return;
        try {
          const payload = JSON.parse(event.data);
          setDashboardData(normalizeDashboard(payload));
        } catch (error) {
          console.error('Failed to parse SSE payload', error);
        }
      };
    };

    connect();

    return () => {
      if (eventSource) eventSource.close();
      if (reconnectTimer) clearTimeout(reconnectTimer);
    };
  }, []);

  const { deviceHealth, areaKpis, areaDistribution, activeAlerts, delayedAlerts, recurrentUnits, highRiskAreas } = dashboardData;

  useEffect(() => {
    if (!activeAlerts.length) {
      if (!hasInitializedAlertsRef.current) {
        hasInitializedAlertsRef.current = true;
      }
      return;
    }

    const currentIds = new Set(activeAlerts.map(alert => alert.id));
    if (!hasInitializedAlertsRef.current) {
      knownAlertIdsRef.current = currentIds;
      hasInitializedAlertsRef.current = true;
      return;
    }

    activeAlerts.forEach(alert => {
      if (!knownAlertIdsRef.current.has(alert.id)) {
        addNotification('New Fatigue Alert!', `Unit ${alert.unit} detected in ${alert.location}`);
      }
    });
    knownAlertIdsRef.current = currentIds;
  }, [activeAlerts]);

  useEffect(() => {
    if (!selectedAlert) return;
    const updatedAlert = [...activeAlerts, ...delayedAlerts].find(alert => alert.id === selectedAlert.id);
    if (updatedAlert) {
      setSelectedAlert(updatedAlert);
    } else {
      setSelectedAlert(null);
    }
  }, [activeAlerts, delayedAlerts, selectedAlert]);

  const areaKpiMap = useMemo(() => {
    const map = {
      All: { totalAlarms: 0, followedUp: 0, waitingFollowUp: 0 },
      Mining: { totalAlarms: 0, followedUp: 0, waitingFollowUp: 0 },
      Hauling: { totalAlarms: 0, followedUp: 0, waitingFollowUp: 0 }
    };
    areaKpis.forEach(kpi => {
      map[kpi.area] = kpi;
    });
    return map;
  }, [areaKpis]);

  const currentKpi = areaKpiMap[selectedArea] ?? areaKpiMap.All;

  const areaSummary = {
    Mining: areaKpiMap.Mining,
    Hauling: areaKpiMap.Hauling
  };

  const stats = useMemo(() => {
    return {
      totalToday: currentKpi.totalAlarms ?? 0,
      followedUpToday: currentKpi.followedUp ?? 0,
      activeOpen: currentKpi.waitingFollowUp ?? 0
    };
  }, [currentKpi]);

  const areaDistributionByArea = useMemo(() => {
    return {
      Mining: areaDistribution.filter(item => item.area === 'Mining'),
      Hauling: areaDistribution.filter(item => item.area === 'Hauling')
    };
  }, [areaDistribution]);

  const filteredActiveAlerts = useMemo(() => {
    return activeAlerts.filter(alert => {
      if (selectedLocationFilter) {
        return alert.location === selectedLocationFilter;
      }
      return selectedArea === 'All' || alert.area === selectedArea;
    });
  }, [activeAlerts, selectedArea, selectedLocationFilter]);

  const filteredDelayedAlerts = useMemo(() => {
    return delayedAlerts.filter(alert => {
      if (selectedLocationFilter) {
        return alert.location === selectedLocationFilter;
      }
      return selectedArea === 'All' || alert.area === selectedArea;
    });
  }, [delayedAlerts, selectedArea, selectedLocationFilter]);

  const filteredRecurrentUnits = useMemo(() => {
    if (selectedArea === 'All') return recurrentUnits;
    return recurrentUnits.filter(unit => unit.area === selectedArea);
  }, [recurrentUnits, selectedArea]);

  const filteredHighRiskAreas = useMemo(() => {
    if (selectedArea === 'All') return highRiskAreas;
    return highRiskAreas.filter(area => area.area === selectedArea);
  }, [highRiskAreas, selectedArea]);

  useEffect(() => {
    const timer = setInterval(() => setCurrentTime(new Date()), 1000);
    return () => clearInterval(timer);
  }, []);

  const getOpenDurationValue = (openedAtUtc) => {
    if (!openedAtUtc) return 0;
    const eventTime = new Date(openedAtUtc);
    if (Number.isNaN(eventTime.getTime())) return 0;
    const diffMs = currentTime - eventTime;
    const diffMins = Math.floor(diffMs / 60000);
    return diffMins < 0 ? 0 : diffMins;
  };

  const getOpenDuration = (openedAtUtc) => {
    return getOpenDurationValue(openedAtUtc);
  };

  const formatTime = (openedAtUtc) => {
    if (!openedAtUtc) return '--:--:--';
    const eventTime = new Date(openedAtUtc);
    if (Number.isNaN(eventTime.getTime())) return '--:--:--';
    return eventTime.toLocaleTimeString('id-ID', { hour12: false, timeZone: 'Asia/Jakarta' });
  };

  const formatSpeed = (speedKph) => {
    if (!Number.isFinite(speedKph)) return 'N/A';
    return `${speedKph.toFixed(1)} km/h`;
  };

  const formatCoordinate = (value) => {
    if (typeof value !== 'number' || Number.isNaN(value)) return 'N/A';
    return value.toFixed(5);
  };

  const filteredAlerts = filteredActiveAlerts;
  const overdueAlerts = filteredDelayedAlerts;

  const handleAreaTabClick = (area) => {
      setSelectedArea(area);
      setSelectedLocationFilter(null);
  };

  // Helper Pagination Function
  const paginate = (data, page, limit) => {
    const start = (page - 1) * limit;
    return data.slice(start, start + limit);
  };

  // Helper Pagination Component (DIPERBESAR)
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
  const getSseStatusColor = () => {
    if (sseStatus === 'Connected') return 'bg-emerald-500';
    if (sseStatus === 'Connecting') return 'bg-amber-400 animate-pulse';
    return 'bg-red-500';
  };

  const getAreaTitle = () => selectedArea === 'All' ? '' : `${selectedArea.toUpperCase()} `;

  return (
    <div className={`h-screen w-screen transition-colors duration-300 font-sans ${getBodyBg()} flex flex-col overflow-hidden relative selection:bg-red-500/30`}>
      
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
                <button onClick={() => removeNotification(notif.id)} className="text-slate-500 hover:text-red-500 shrink-0">
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
                            Unit <span className="font-mono font-bold bg-slate-700 text-white px-3 py-1 rounded mx-2">{selectedAlert.unit}</span> 
                            operated by <strong className="text-red-400">{selectedAlert.operator}</strong>
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
                        <div className="flex flex-col gap-[2vh] h-full">
                            <h3 className={`font-bold text-xl uppercase tracking-wider flex items-center gap-3 shrink-0 ${darkMode ? 'text-slate-200' : 'opacity-70'}`}>
                                <Camera size={28}/> In-Cabin Camera Feed
                            </h3>
                            <div className="flex-1 relative rounded-2xl bg-black border-4 border-slate-700 overflow-hidden group">
                                <div className="absolute inset-0 bg-green-900/10 z-10 pointer-events-none mix-blend-overlay"></div>
                                <div className="absolute inset-0 bg-[radial-gradient(circle,transparent_40%,#000_100%)] z-20 pointer-events-none"></div>
                                <div className="absolute top-[25%] left-[30%] right-[30%] bottom-[25%] border-4 border-red-500/80 rounded-xl z-30 shadow-[0_0_20px_rgba(239,68,68,0.5)] animate-pulse flex flex-col items-center justify-end pb-8">
                                     <div className="bg-red-600 text-white text-xl font-bold px-6 py-3 rounded-lg flex items-center gap-3 shadow-lg">
                                        <EyeOff size={24} /> EYES CLOSED (2.5s)
                                     </div>
                                </div>
                            </div>
                            <div className="grid grid-cols-2 gap-4 shrink-0 h-[12vh]">
                                 <div className={`p-6 rounded-xl border flex flex-col justify-center ${darkMode ? 'bg-slate-800 border-slate-700' : 'bg-white border-slate-300'}`}>
                                    <span className={`text-sm block uppercase tracking-wider font-bold mb-1 ${darkMode ? 'text-slate-300' : 'opacity-60'}`}>Vehicle Speed</span>
                                    <span className="font-mono font-bold text-5xl">{formatSpeed(selectedAlert.speedKph)}</span>
                                 </div>
                                 <div className={`p-6 rounded-xl border flex flex-col justify-center ${darkMode ? 'bg-slate-800 border-slate-700' : 'bg-white border-slate-300'}`}>
                                    <span className={`text-sm block uppercase tracking-wider font-bold mb-1 ${darkMode ? 'text-slate-300' : 'opacity-60'}`}>Fatigue Type</span>
                                    <span className="font-bold text-4xl text-red-500">{selectedAlert.alarmType || 'Fatigue'}</span>
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
                                    <span className="font-mono text-2xl font-medium text-blue-500">Lat: {formatCoordinate(selectedAlert.latitude)}, Long: {formatCoordinate(selectedAlert.longitude)}</span>
                                    <div className={`text-sm mt-2 ${darkMode ? 'text-slate-400' : 'opacity-50'}`}>Accuracy: ±2m</div>
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

      {/* --- HEADER (Fixed Height: 8vh) --- */}
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
          <div className={`flex items-center gap-3 px-[1.2vw] py-[0.8vh] rounded-full border ${darkMode ? 'bg-slate-800 border-slate-700' : 'bg-white border-slate-300 shadow-sm'}`}>
            <div className={`w-3 h-3 lg:w-4 lg:h-4 rounded-full ${getSseStatusColor()}`}></div>
            <span className={`text-[clamp(0.9rem,1.1vh,1.3rem)] font-semibold whitespace-nowrap ${darkMode ? 'text-slate-200' : 'text-slate-700'}`}>
              SSE: {sseStatus}
            </span>
          </div>

          <div className={`hidden md:flex items-center gap-6 px-[1.5vw] py-[1vh] rounded-full border ${darkMode ? 'bg-slate-800 border-slate-700' : 'bg-white border-slate-300 shadow-sm'}`}>
            <div className="flex items-center gap-3">
              <div className={`w-3 h-3 lg:w-4 lg:h-4 rounded-full ${deviceHealth.offlineDevices === 0 ? 'bg-emerald-500' : 'bg-amber-500 animate-pulse'}`}></div>
              <span className={`text-[clamp(1rem,1.2vh,1.5rem)] font-semibold whitespace-nowrap ${darkMode ? 'text-slate-200' : 'text-slate-700'}`}>Sensor Health: {deviceHealth.coveragePercent}%</span>
            </div>
            <div className="h-6 w-px bg-slate-600/30"></div>
            <div className="flex items-center gap-4 text-[clamp(1rem,1.2vh,1.5rem)]">
              <div className="flex items-center gap-2"><Wifi className="w-[2.5vh] h-[2.5vh] text-emerald-500" /> <span className={`font-mono font-bold ${darkMode ? 'text-white' : ''}`}>{deviceHealth.onlineDevices}</span></div>
              <div className="flex items-center gap-2"><WifiOff className="w-[2.5vh] h-[2.5vh] text-red-500 ml-1" /> <span className={`font-mono font-bold ${darkMode ? 'text-white' : ''}`}>{deviceHealth.offlineDevices}</span></div>
            </div>
          </div>

          <div className={`flex flex-col items-end ${darkMode ? 'text-white' : 'text-slate-600'}`}>
            <span className="text-[clamp(1.8rem,3vh,3.5rem)] font-mono font-bold leading-none">
              {currentTime.toLocaleTimeString('id-ID', { hour: '2-digit', minute: '2-digit', second: '2-digit' })} WIB
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
        
        {/* --- LEFT PANEL & MIDDLE (Flex-4) --- */}
        <div className="flex-[4] flex flex-col gap-[2vh] min-w-0 h-full">
            
            {/* 1. KPI CARDS (Height ~16vh) */}
            <div className="flex gap-[2vh] shrink-0 h-[16vh] min-h-[140px]">
                
                {/* WRAPPER 1: ALIGN WITH AREA DISTRIBUTION (FLEX 1.8) */}
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

                {/* WRAPPER 2: ALIGN WITH DELAYED (FLEX 1) */}
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

            {/* 2. MIDDLE AREA (Flex-1.5) UPDATED PROPORTION */}
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
                                            <span className={darkMode ? 'text-slate-200 font-bold' : 'text-slate-600'}>{areaSummary.Mining.totalAlarms} Total</span>
                                            <span className="text-red-500 font-bold">{areaSummary.Mining.waitingFollowUp} Open</span>
                                        </div>
                                    </div>
                                    <div className="flex-1 flex flex-col justify-between overflow-hidden" ref={miningListContainerRef}>
                                        <div className="grid grid-cols-2 gap-3 mt-1">
                                            {areaDistributionByArea.Mining.length > 0 ? (
                                                paginate(areaDistributionByArea.Mining, miningPage, dynamicItemsPerPage.mining).map(({ location, openCount }) => (
                                                    <div key={location} onClick={() => { setSelectedArea('Mining'); setSelectedLocationFilter(location); }} className={`p-3 rounded-xl border flex flex-col justify-between items-center text-center transition-all cursor-pointer hover:scale-105 ${selectedLocationFilter === location ? (darkMode ? 'bg-blue-900/40 border-blue-400 ring-1 ring-blue-500' : 'bg-blue-50 border-blue-500 ring-1 ring-blue-200') : (darkMode ? 'bg-slate-800 border-slate-600 hover:border-slate-500' : 'bg-white border-slate-200 shadow-sm hover:border-blue-300')}`}>
                                                        <span className={`text-[clamp(0.9rem,1.2vh,1.4rem)] font-medium mb-1 line-clamp-1 ${darkMode ? 'text-white' : 'text-slate-600'}`}>{location}</span>
                                                        <span className="text-[clamp(1.8rem,2.5vh,3rem)] font-black text-red-500 leading-none">{openCount}</span>
                                                    </div>
                                                ))
                                            ) : <div className="col-span-2 py-4 text-center text-slate-500 text-lg italic">No active alerts</div>}
                                        </div>
                                        <PaginationControls currentPage={miningPage} totalPages={Math.ceil(areaDistributionByArea.Mining.length / dynamicItemsPerPage.mining)} onPageChange={setMiningPage} />
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
                                            <span className={darkMode ? 'text-slate-200 font-bold' : 'text-slate-600'}>{areaSummary.Hauling.totalAlarms} Total</span>
                                            <span className="text-red-500 font-bold">{areaSummary.Hauling.waitingFollowUp} Open</span>
                                        </div>
                                    </div>
                                    <div className="flex-1 flex flex-col justify-between overflow-hidden" ref={haulingListContainerRef}>
                                        <div className="grid grid-cols-2 gap-3 mt-1">
                                            {areaDistributionByArea.Hauling.length > 0 ? (
                                                paginate(areaDistributionByArea.Hauling, haulingPage, dynamicItemsPerPage.hauling).map(({ location, openCount }) => (
                                                    <div key={location} onClick={() => { setSelectedArea('Hauling'); setSelectedLocationFilter(location); }} className={`p-3 rounded-xl border flex flex-col justify-between items-center text-center transition-all cursor-pointer hover:scale-105 ${selectedLocationFilter === location ? (darkMode ? 'bg-teal-900/40 border-teal-400 ring-1 ring-teal-500' : 'bg-teal-50 border-teal-500 ring-1 ring-teal-200') : (darkMode ? 'bg-slate-800 border-slate-600 hover:border-slate-500' : 'bg-white border-slate-200 shadow-sm hover:border-teal-300')}`}>
                                                        <span className={`text-[clamp(0.9rem,1.2vh,1.4rem)] font-medium mb-1 line-clamp-1 ${darkMode ? 'text-white' : 'text-slate-600'}`}>{location}</span>
                                                        <span className="text-[clamp(1.8rem,2.5vh,3rem)] font-black text-red-500 leading-none">{openCount}</span>
                                                    </div>
                                                ))
                                            ) : <div className="col-span-2 py-4 text-center text-slate-500 text-lg italic">No active alerts</div>}
                                        </div>
                                        <PaginationControls currentPage={haulingPage} totalPages={Math.ceil(areaDistributionByArea.Hauling.length / dynamicItemsPerPage.hauling)} onPageChange={setHaulingPage} />
                                    </div>
                                </div>
                            )}
                        </div>
                    </div>
                </div>

                {/* DELAYED FOLLOW UP (Paginated) */}
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
                                    <div key={alert.id} onClick={() => setSelectedAlert(alert)} className={`relative group p-3 rounded-xl border flex justify-between items-center cursor-pointer transition-all hover:bg-red-500/10 hover:border-red-400 ${darkMode ? 'bg-slate-900 border-red-500/30' : 'bg-white border-red-200 shadow-sm'}`}>
                                        <div>
                                            <div className={`font-bold text-[clamp(1rem,1.2vh,1.4rem)] ${darkMode ? 'text-white' : 'text-slate-900'}`}>{alert.unit}</div>
                                            <div className={`text-[1rem] ${darkMode ? 'text-slate-300' : 'text-slate-500'}`}>{alert.location}</div>
                                        </div>
                                        <div className="text-right">
                                            <div className="text-[1.2rem] font-black text-red-500 font-mono">+{getOpenDurationValue(alert.openedAtUtc)}m</div>
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

            {/* 3. STRATEGIC INSIGHTS (Flex-1.2) UPDATED PROPORTION */}
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
                    <span className="bg-red-600 text-white text-[1rem] font-bold px-3 py-1 rounded-full">{filteredRecurrentUnits.length} Total</span>
                  </div>
                  <div className="flex-1 flex flex-col justify-between overflow-hidden" ref={recurrentListContainerRef}>
                    <div className="flex-1 space-y-2">
                        {filteredRecurrentUnits.length > 0 ? (
                            paginate(filteredRecurrentUnits, recurrentPage, dynamicItemsPerPage.recurrent).map((op, idx) => (
                            <div key={idx} className={`px-3 py-2 rounded-xl border-l-4 border-red-500 flex justify-between items-center ${darkMode ? 'bg-slate-900/50' : 'bg-white border border-slate-200'}`}>
                                <div>
                                    <div className={`font-bold text-[clamp(1rem,1.2vh,1.4rem)] ${darkMode ? 'text-white' : 'text-slate-800'}`}>{op.unit}</div>
                                    <div className={`text-[0.9rem] ${darkMode ? 'text-slate-300' : 'text-slate-500'}`}>{op.operator} • <span className="text-red-500 font-semibold">{op.events} events</span></div>
                                </div>
                                <div className="text-[0.9rem] text-red-500 font-bold px-3 py-1 bg-red-500/10 rounded">MONITORING</div>
                            </div>
                            ))
                        ) : <div className="flex h-full items-center justify-center text-slate-500 text-lg">No recurrent data</div>}
                    </div>
                    <PaginationControls currentPage={recurrentPage} totalPages={Math.ceil(filteredRecurrentUnits.length / dynamicItemsPerPage.recurrent)} onPageChange={setRecurrentPage} />
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
                    <span className="bg-orange-600 text-white text-[1rem] font-bold px-3 py-1 rounded-full">{filteredHighRiskAreas.length} Total</span>
                  </div>
                  <div className="flex-1 flex flex-col justify-between overflow-hidden" ref={highRiskListContainerRef}>
                        <div className="flex-1 space-y-2">
                            {filteredHighRiskAreas.length > 0 ? (
                            paginate(filteredHighRiskAreas, highRiskPage, dynamicItemsPerPage.highRisk).map((zone, idx) => (
                            <div key={idx} className={`px-3 py-2 rounded-xl border flex justify-between items-center ${darkMode ? 'bg-slate-900/50 border-slate-700' : 'bg-white border-slate-200'}`}>
                                <div>
                                    <div className="flex items-center gap-2">
                                        <span className="w-3 h-3 rounded-full bg-red-500"></span>
                                        <span className={`text-[clamp(1rem,1.2vh,1.4rem)] font-bold ${darkMode ? 'text-white' : 'text-slate-800'}`}>{zone.location}</span>
                                    </div>
                                    <div className={`text-[0.9rem] ml-5 ${darkMode ? 'text-slate-300' : 'text-slate-500'}`}>Events: <span className="font-mono font-bold text-slate-400">{zone.events}</span></div>
                                </div>
                                <div className={`text-[0.9rem] px-3 py-1 rounded border flex items-center gap-2 font-medium ${zone.area === 'Mining' ? 'bg-blue-500/10 text-blue-500 border-blue-500/20' : 'bg-teal-500/10 text-teal-500 border-teal-500/20'}`}>
                                    {zone.area === 'Mining' ? <Activity size={16} /> : <Truck size={16} />} {zone.area.toUpperCase()}
                                </div>
                            </div>
                            ))
                            ) : <div className="flex h-full items-center justify-center text-slate-500 text-lg">No high risk data</div>}
                        </div>
                        <PaginationControls currentPage={highRiskPage} totalPages={Math.ceil(filteredHighRiskAreas.length / dynamicItemsPerPage.highRisk)} onPageChange={setHighRiskPage} />
                    </div>
                </div>
            </div>
        </div>

        {/* --- RIGHT PANEL: GLOBAL FILTER & ACTIVE FATIGUE LIST (Flex-1) --- */}
        <div className="flex-1 flex flex-col gap-[2vh] min-w-[320px] lg:min-w-[400px] h-full">
            
            {/* 1. GLOBAL FILTER CARD (Height ~16vh) */}
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

            {/* 2. ACTIVE FATIGUE RECENT LIST (PAGINATED) */}
            <div className={`flex-1 flex flex-col rounded-2xl border overflow-hidden ${getCardBg()}`}>
                <div className="p-[2.5vh] border-b border-inherit shrink-0">
                    <div className="flex items-center justify-between mb-2">
                        <div className="flex items-center gap-3">
                            <Zap className="text-yellow-500 w-[3.5vh] h-[3.5vh]" />
                            <h3 className={`font-bold text-[clamp(1.5rem,2vh,2.5rem)] ${darkMode ? 'text-white' : 'text-slate-800'}`}>Active Alerts</h3>
                        </div>
                        <span className={`text-lg font-bold ${darkMode ? 'text-white' : 'text-slate-600'}`}>{filteredAlerts.length} Total</span>
                    </div>
                    <div className={`flex justify-between items-center text-[1rem] ${darkMode ? 'text-slate-300' : 'opacity-60'}`}><span>Recent Alerts</span><span className="text-red-500 font-bold">OPEN ONLY</span></div>
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
                                        <span className={`px-2 py-1 rounded-md text-[0.9rem] font-bold uppercase bg-red-600 text-white`}>{alert.alarmType || 'Fatigue'}</span>
                                        <span className={`text-[1rem] font-mono ${darkMode ? 'text-slate-300' : 'text-slate-500'}`}>{formatTime(alert.openedAtUtc)}</span>
                                    </div>
                                    <span className="text-[0.9rem] text-red-500 font-bold flex items-center gap-2 animate-pulse"><AlertTriangle size={14} /> ACTIVE</span>
                                </div>
                                <div className="flex items-center gap-3 mb-2">
                                    <div className={`p-2 rounded-xl ${darkMode ? 'bg-slate-700' : 'bg-slate-100'}`}><Truck className={`w-[3vh] h-[3vh] ${darkMode ? 'text-slate-300' : 'text-slate-600'}`} /></div>
                                    <div>
                                        <h4 className={`font-bold text-[clamp(1.2rem,1.5vh,1.8rem)] ${darkMode ? 'text-slate-200' : 'text-slate-800'}`}>{alert.unit}</h4>
                                        <p className={`text-[1rem] ${darkMode ? 'text-slate-300' : 'text-slate-500'}`}>{alert.operator} • {(alert.occurrences ?? 1)}x Today</p>
                                    </div>
                                </div>
                                <div className={`text-[1rem] p-2 rounded-lg flex justify-between items-center ${darkMode ? 'bg-slate-900' : 'bg-slate-100'}`}>
                                    <div className={`flex items-center gap-2 font-medium ${darkMode ? 'text-slate-300' : 'text-slate-500'}`}><Map size={18} /><span>{alert.location}</span></div>
                                    <div className="flex items-center gap-2 text-red-400 font-mono font-bold animate-pulse"><Clock size={18} /><span>+{getOpenDuration(alert.openedAtUtc)}m</span></div>
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