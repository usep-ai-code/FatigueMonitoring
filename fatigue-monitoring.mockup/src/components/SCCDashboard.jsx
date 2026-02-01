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
  Navigation,
  Users,
  Wifi,
  WifiOff,
  RefreshCw,
  MapPin,
  Siren,
  ShieldAlert,
  Bell,
  X,
  LayoutGrid,
  Timer,
  Maximize2,
  Camera,
  EyeOff,
  Filter,
  ChevronLeft,
  ChevronRight
} from 'lucide-react';

const SCCDashboard = () => {
  const [darkMode, setDarkMode] = useState(true);
  const [currentTime, setCurrentTime] = useState(new Date());
  const [selectedArea, setSelectedArea] = useState('All'); 
  const [isSyncing, setIsSyncing] = useState(false);
  
  const [notifications, setNotifications] = useState([]);
  const [selectedAlert, setSelectedAlert] = useState(null);
  const [selectedLocationFilter, setSelectedLocationFilter] = useState(null);

  // --- REFS UNTUK MENGUKUR TINGGI CONTAINER (DYNAMIC PAGINATION) ---
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

  const getTimeAgo = (minutes) => {
    const d = new Date();
    d.setMinutes(d.getMinutes() - minutes);
    return d.toLocaleTimeString('en-GB', { hour12: false });
  };

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

  // --- MOCK DATA ---
  const [alerts, setAlerts] = useState([
    { id: 101, unit: 'DT-402', operator: 'Budi S.', type: 'Fatigue', area: 'Mining', location: 'Manado - Front A', time: getTimeAgo(45), status: 'Open', speed: '0 km/h', count: 2 },
    { id: 102, unit: 'DT-112', operator: 'Dedi S.', type: 'Fatigue', area: 'Hauling', location: 'KM 22', time: getTimeAgo(35), status: 'Open', speed: '38 km/h', count: 1 },
    { id: 103, unit: 'DT-555', operator: 'Rian J.', type: 'Fatigue', area: 'Mining', location: 'Pit Utara', time: getTimeAgo(58), status: 'Open', speed: '0 km/h', count: 3 },
    { id: 104, unit: 'HD-777', operator: 'Doni K.', type: 'Fatigue', area: 'Hauling', location: 'KM 12', time: getTimeAgo(42), status: 'Open', speed: '40 km/h', count: 2 },
    { id: 105, unit: 'EX-202', operator: 'Yanto', type: 'Fatigue', area: 'Mining', location: 'Loading Point C', time: getTimeAgo(32), status: 'Open', speed: '0 km/h', count: 1 },
    { id: 1, unit: 'DT-315', operator: 'Agus R.', type: 'Fatigue', area: 'Hauling', location: 'KM 30', time: getTimeAgo(5), status: 'Open', speed: '45 km/h', count: 1 },
    { id: 3, unit: 'EX-201', operator: 'Dedi K.', type: 'Fatigue', area: 'Mining', location: 'Manado - Loading', time: getTimeAgo(15), status: 'Followed Up', speed: '0 km/h', count: 1 },
    { id: 5, unit: 'GD-102', operator: 'Rudi H.', type: 'Fatigue', area: 'Mining', location: 'Manado - Disposal', time: getTimeAgo(10), status: 'Open', speed: '15 km/h', count: 1 },
    { id: 6, unit: 'DT-399', operator: 'Eko P.', type: 'Fatigue', area: 'Mining', location: 'Manado - Ramp B', time: getTimeAgo(25), status: 'Open', speed: '18 km/h', count: 3 },
    { id: 7, unit: 'WT-05', operator: 'Joko', type: 'Fatigue', area: 'Hauling', location: 'KM 45', time: getTimeAgo(2), status: 'Followed Up', speed: '30 km/h', count: 1 },
    { id: 9, unit: 'DT-551', operator: 'Iwan', type: 'Fatigue', area: 'Mining', location: 'Manado - Front A', time: getTimeAgo(8), status: 'Open', speed: '10 km/h', count: 1 },
    { id: 10, unit: 'DT-101', operator: 'Slamet', type: 'Fatigue', area: 'Mining', location: 'Manado - Front B', time: getTimeAgo(1), status: 'Open', speed: '12 km/h', count: 1 },
    { id: 12, unit: 'DT-103', operator: 'Tono', type: 'Fatigue', area: 'Mining', location: 'Manado - Front C', time: getTimeAgo(4), status: 'Open', speed: '0 km/h', count: 1 },
    
    { id: 11, unit: 'DT-102', operator: 'Udin', type: 'Fatigue', area: 'Hauling', location: 'KM 10', time: getTimeAgo(3), status: 'Open', speed: '40 km/h', count: 1 },
    { id: 14, unit: 'DT-205', operator: 'Asep K.', type: 'Fatigue', area: 'Hauling', location: 'KM 55', time: getTimeAgo(12), status: 'Open', speed: '42 km/h', count: 1 },
    
    { id: 21, unit: 'DT-999', operator: 'Recurrent Op 1', type: 'Fatigue', area: 'Hauling', location: 'KM 30', time: getTimeAgo(10), status: 'Open', speed: '30 km/h', count: 4 },
    { id: 22, unit: 'DT-888', operator: 'Recurrent Op 2', type: 'Fatigue', area: 'Mining', location: 'Manado - Front A', time: getTimeAgo(5), status: 'Open', speed: '0 km/h', count: 5 },
    { id: 23, unit: 'DT-777', operator: 'Recurrent Op 3', type: 'Fatigue', area: 'Hauling', location: 'KM 10', time: getTimeAgo(2), status: 'Open', speed: '35 km/h', count: 3 },
    { id: 24, unit: 'DT-666', operator: 'Recurrent Op 4', type: 'Fatigue', area: 'Mining', location: 'Pit Utara', time: getTimeAgo(1), status: 'Open', speed: '0 km/h', count: 2 },
    { id: 25, unit: 'DT-555', operator: 'Recurrent Op 5', type: 'Fatigue', area: 'Mining', location: 'Manado - Front A', time: getTimeAgo(3), status: 'Open', speed: '0 km/h', count: 2 },
    { id: 26, unit: 'DT-444', operator: 'Recurrent Op 6', type: 'Fatigue', area: 'Hauling', location: 'KM 22', time: getTimeAgo(4), status: 'Open', speed: '40 km/h', count: 3 },
    
    { id: 301, unit: 'DT-Delayed1', operator: 'Late 1', type: 'Fatigue', area: 'Mining', location: 'Manado - Front A', time: getTimeAgo(35), status: 'Open', speed: '0 km/h', count: 1 },
    { id: 302, unit: 'DT-Delayed2', operator: 'Late 2', type: 'Fatigue', area: 'Hauling', location: 'KM 22', time: getTimeAgo(40), status: 'Open', speed: '20 km/h', count: 1 },
    { id: 303, unit: 'DT-Delayed3', operator: 'Late 3', type: 'Fatigue', area: 'Mining', location: 'Pit Utara', time: getTimeAgo(60), status: 'Open', speed: '0 km/h', count: 1 },
  ]);

  const filteredAlertsByArea = useMemo(() => {
      return selectedArea === 'All' 
        ? alerts 
        : alerts.filter(a => a.area === selectedArea);
  }, [alerts, selectedArea]);

  const highRiskOperators = useMemo(() => {
    const opMap = {};
    filteredAlertsByArea.forEach(a => {
        if (!opMap[a.operator]) {
            opMap[a.operator] = { name: a.operator, unit: a.unit, events: 0, status: 'Active' }; 
        }
        opMap[a.operator].events += a.count;
    });

    return Object.values(opMap)
        .filter(op => op.events > 1)
        .sort((a, b) => b.events - a.events);
  }, [filteredAlertsByArea]);

  const highFreqZones = useMemo(() => {
    const zoneMap = {};
    filteredAlertsByArea.forEach(a => {
        if (!zoneMap[a.location]) {
            zoneMap[a.location] = { location: a.location, count: 0, area: a.area };
        }
        zoneMap[a.location].count += 1;
    });

    return Object.values(zoneMap)
        .sort((a, b) => b.count - a.count);
  }, [filteredAlertsByArea]);

  const deviceHealth = {
    total: 142,
    online: 135,
    offline: 7,
    coverage: 95
  };

  useEffect(() => {
    const timer = setInterval(() => setCurrentTime(new Date()), 1000);
    return () => clearInterval(timer);
  }, []);

  useEffect(() => {
    const interval = setInterval(() => {
      const areas = ['Mining', 'Hauling'];
      const randomArea = areas[Math.floor(Math.random() * areas.length)];
      const uniqueId = Date.now() + Math.floor(Math.random() * 100); 
      
      const newAlert = {
        id: uniqueId,
        unit: `DT-${Math.floor(Math.random() * 900) + 100}`, 
        operator: 'Driver Baru',
        type: 'Fatigue',
        area: randomArea,
        location: randomArea === 'Mining' ? `Manado - Front ${['A','B','C'][Math.floor(Math.random()*3)]}` : `KM ${Math.floor(Math.random() * 60)}`,
        time: new Date().toLocaleTimeString('en-GB', { hour12: false }),
        status: 'Open',
        speed: `${Math.floor(Math.random() * 40) + 10} km/h`,
        count: 1
      };

      setAlerts(prev => {
        if (prev.some(a => a.id === uniqueId)) return prev;
        return [newAlert, ...prev];
      });
      
      addNotification('New Fatigue Alert!', `Unit ${newAlert.unit} detected in ${newAlert.location}`);

    }, 20000); 

    return () => clearInterval(interval);
  }, []);

  const getOpenDurationValue = (timeStr) => {
    const parts = timeStr.split(/[:.]/); 
    const h = parseInt(parts[0], 10);
    const m = parseInt(parts[1], 10);
    const s = parseInt(parts[2] || '0', 10);

    if (isNaN(h) || isNaN(m)) return 0;

    const eventTime = new Date();
    eventTime.setHours(h, m, s);
    
    const diffMs = currentTime - eventTime;
    const diffMins = Math.floor(diffMs / 60000);
    return diffMins < 0 ? 0 : diffMins;
  };

  const getOpenDuration = (timeStr) => {
    return getOpenDurationValue(timeStr);
  };

  const filteredAlerts = useMemo(() => {
    return alerts.filter(alert => {
      if (selectedLocationFilter) {
          return alert.location === selectedLocationFilter && alert.status === 'Open';
      }
      const areaMatch = selectedArea === 'All' || alert.area === selectedArea;
      const statusMatch = alert.status === 'Open'; 
      return areaMatch && statusMatch;
    });
  }, [alerts, selectedArea, selectedLocationFilter]);

  const overdueAlerts = useMemo(() => {
    return filteredAlertsByArea.filter(alert => {
      return alert.status === 'Open' && getOpenDurationValue(alert.time) > 30;
    });
  }, [filteredAlertsByArea, currentTime]); 

  const areaSummary = useMemo(() => {
    const summary = {
      Mining: { open: 0, resolved: 0, total: 0 },
      Hauling: { open: 0, resolved: 0, total: 0 }
    };

    alerts.forEach(alert => {
      if (summary[alert.area]) {
        summary[alert.area].total += 1;
        if (alert.status === 'Open') {
          summary[alert.area].open += 1;
        } else if (alert.status === 'Followed Up') {
          summary[alert.area].resolved += 1;
        }
      }
    });

    return summary;
  }, [alerts]);

  const locationStats = useMemo(() => {
    const stats = { Mining: {}, Hauling: {} };
    const openAlerts = alerts.filter(a => a.status === 'Open');

    openAlerts.forEach(alert => {
      if (stats[alert.area]) {
        if (!stats[alert.area][alert.location]) {
          stats[alert.area][alert.location] = 0;
        }
        stats[alert.area][alert.location]++;
      }
    });
    return stats;
  }, [alerts]);

  const stats = useMemo(() => {
    const baseData = filteredAlertsByArea; 
    
    const totalToday = baseData.length; 
    const activeOpen = baseData.filter(a => a.status === 'Open').length; 
    const followedUpToday = baseData.filter(a => a.status === 'Followed Up').length;
    return { totalToday, followedUpToday, activeOpen };
  }, [filteredAlertsByArea]); 

  const handleManualSync = () => {
    setIsSyncing(true);
    setTimeout(() => {
        setAlerts(prev => {
            const openAlerts = prev.filter(a => a.status === 'Open');
            if (openAlerts.length > 0) {
                const randomAlert = openAlerts[Math.floor(Math.random() * openAlerts.length)];
                return prev.map(a => a.id === randomAlert.id ? { ...a, status: 'Followed Up' } : a);
            }
            return prev;
        });
        setIsSyncing(false);
    }, 1500);
  };

  const simulateBurst = () => {
    const newAlerts = [];
    const areas = ['Mining', 'Hauling'];
    
    for (let i = 0; i < 5; i++) {
        const randomArea = areas[Math.floor(Math.random() * areas.length)];
        const uniqueId = Date.now() + i + Math.floor(Math.random() * 1000);
        
        const newAlert = {
            id: uniqueId,
            unit: `DT-${Math.floor(Math.random() * 900) + 100}`, 
            operator: `Simulated Driver ${i+1}`,
            type: 'Fatigue',
            area: randomArea,
            location: randomArea === 'Mining' ? `Manado - Front ${['A','B','C'][Math.floor(Math.random()*3)]}` : `KM ${Math.floor(Math.random() * 60)}`,
            time: new Date().toLocaleTimeString('en-GB', { hour12: false }),
            status: 'Open',
            speed: `${Math.floor(Math.random() * 40) + 10} km/h`,
            count: 1
        };
        newAlerts.push(newAlert);
        setTimeout(() => {
            addNotification('CRITICAL BURST!', `Unit ${newAlert.unit} - ${newAlert.location}`, 'critical');
        }, i * 300); 
    }
    setAlerts(prev => [...newAlerts, ...prev]);
  };

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
                                    <span className="font-mono font-bold text-5xl">{selectedAlert.speed}</span>
                                 </div>
                                 <div className={`p-6 rounded-xl border flex flex-col justify-center ${darkMode ? 'bg-slate-800 border-slate-700' : 'bg-white border-slate-300'}`}>
                                    <span className={`text-sm block uppercase tracking-wider font-bold mb-1 ${darkMode ? 'text-slate-300' : 'opacity-60'}`}>Fatigue Type</span>
                                    <span className="font-bold text-4xl text-red-500">Microsleep</span>
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
                                    <span className="font-mono text-2xl font-medium text-blue-500">Lat: -2.341, Long: 115.421</span>
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
          <button 
              onClick={simulateBurst}
              className={`p-[1vh] rounded-full transition-all hover:bg-slate-700 active:scale-95 text-amber-500 border border-amber-500/30`}
              title="Simulate Burst"
          >
              <Zap size={32} className="w-[3vh] h-[3vh] fill-amber-500" />
          </button>

          <div className={`hidden md:flex items-center gap-6 px-[1.5vw] py-[1vh] rounded-full border ${darkMode ? 'bg-slate-800 border-slate-700' : 'bg-white border-slate-300 shadow-sm'}`}>
            <div className="flex items-center gap-3">
              <div className={`w-3 h-3 lg:w-4 lg:h-4 rounded-full ${deviceHealth.offline === 0 ? 'bg-emerald-500' : 'bg-amber-500 animate-pulse'}`}></div>
              <span className={`text-[clamp(1rem,1.2vh,1.5rem)] font-semibold whitespace-nowrap ${darkMode ? 'text-slate-200' : 'text-slate-700'}`}>Sensor Health: {deviceHealth.coverage}%</span>
            </div>
            <div className="h-6 w-px bg-slate-600/30"></div>
            <div className="flex items-center gap-4 text-[clamp(1rem,1.2vh,1.5rem)]">
              <div className="flex items-center gap-2"><Wifi className="w-[2.5vh] h-[2.5vh] text-emerald-500" /> <span className={`font-mono font-bold ${darkMode ? 'text-white' : ''}`}>{deviceHealth.online}</span></div>
              <div className="flex items-center gap-2"><WifiOff className="w-[2.5vh] h-[2.5vh] text-red-500 ml-1" /> <span className={`font-mono font-bold ${darkMode ? 'text-white' : ''}`}>{deviceHealth.offline}</span></div>
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
                                            <div className="text-[1.2rem] font-black text-red-500 font-mono">+{getOpenDurationValue(alert.time)}m</div>
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
                    <span className="bg-red-600 text-white text-[1rem] font-bold px-3 py-1 rounded-full">{highRiskOperators.length} Total</span>
                  </div>
                  <div className="flex-1 flex flex-col justify-between overflow-hidden" ref={recurrentListContainerRef}>
                    <div className="flex-1 space-y-2">
                        {highRiskOperators.length > 0 ? (
                            paginate(highRiskOperators, recurrentPage, dynamicItemsPerPage.recurrent).map((op, idx) => (
                            <div key={idx} className={`px-3 py-2 rounded-xl border-l-4 border-red-500 flex justify-between items-center ${darkMode ? 'bg-slate-900/50' : 'bg-white border border-slate-200'}`}>
                                <div>
                                    <div className={`font-bold text-[clamp(1rem,1.2vh,1.4rem)] ${darkMode ? 'text-white' : 'text-slate-800'}`}>{op.unit}</div>
                                    <div className={`text-[0.9rem] ${darkMode ? 'text-slate-300' : 'text-slate-500'}`}>{op.name} • <span className="text-red-500 font-semibold">{op.events} events</span></div>
                                </div>
                                <div className="text-[0.9rem] text-red-500 font-bold px-3 py-1 bg-red-500/10 rounded">MONITORING</div>
                            </div>
                            ))
                        ) : <div className="flex h-full items-center justify-center text-slate-500 text-lg">No recurrent data</div>}
                    </div>
                    <PaginationControls currentPage={recurrentPage} totalPages={Math.ceil(highRiskOperators.length / dynamicItemsPerPage.recurrent)} onPageChange={setRecurrentPage} />
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
                  <div className="flex-1 flex flex-col justify-between overflow-hidden" ref={highRiskListContainerRef}>
                        <div className="flex-1 space-y-2">
                            {highFreqZones.length > 0 ? (
                            paginate(highFreqZones, highRiskPage, dynamicItemsPerPage.highRisk).map((zone, idx) => (
                            <div key={idx} className={`px-3 py-2 rounded-xl border flex justify-between items-center ${darkMode ? 'bg-slate-900/50 border-slate-700' : 'bg-white border-slate-200'}`}>
                                <div>
                                    <div className="flex items-center gap-2">
                                        <span className="w-3 h-3 rounded-full bg-red-500"></span>
                                        <span className={`text-[clamp(1rem,1.2vh,1.4rem)] font-bold ${darkMode ? 'text-white' : 'text-slate-800'}`}>{zone.location}</span>
                                    </div>
                                    <div className={`text-[0.9rem] ml-5 ${darkMode ? 'text-slate-300' : 'text-slate-500'}`}>Events: <span className="font-mono font-bold text-slate-400">{zone.count}</span></div>
                                </div>
                                <div className={`text-[0.9rem] px-3 py-1 rounded border flex items-center gap-2 font-medium ${zone.area === 'Mining' ? 'bg-blue-500/10 text-blue-500 border-blue-500/20' : 'bg-teal-500/10 text-teal-500 border-teal-500/20'}`}>
                                    {zone.area === 'Mining' ? <Activity size={16} /> : <Truck size={16} />} {zone.area.toUpperCase()}
                                </div>
                            </div>
                            ))
                            ) : <div className="flex h-full items-center justify-center text-slate-500 text-lg">No high risk data</div>}
                        </div>
                        <PaginationControls currentPage={highRiskPage} totalPages={Math.ceil(highFreqZones.length / dynamicItemsPerPage.highRisk)} onPageChange={setHighRiskPage} />
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
                                        <span className={`px-2 py-1 rounded-md text-[0.9rem] font-bold uppercase bg-red-600 text-white`}>{alert.type}</span>
                                        <span className={`text-[1rem] font-mono ${darkMode ? 'text-slate-300' : 'text-slate-500'}`}>{alert.time}</span>
                                    </div>
                                    <span className="text-[0.9rem] text-red-500 font-bold flex items-center gap-2 animate-pulse"><AlertTriangle size={14} /> ACTIVE</span>
                                </div>
                                <div className="flex items-center gap-3 mb-2">
                                    <div className={`p-2 rounded-xl ${darkMode ? 'bg-slate-700' : 'bg-slate-100'}`}><Truck className={`w-[3vh] h-[3vh] ${darkMode ? 'text-slate-300' : 'text-slate-600'}`} /></div>
                                    <div>
                                        <h4 className={`font-bold text-[clamp(1.2rem,1.5vh,1.8rem)] ${darkMode ? 'text-slate-200' : 'text-slate-800'}`}>{alert.unit}</h4>
                                        <p className={`text-[1rem] ${darkMode ? 'text-slate-300' : 'text-slate-500'}`}>{alert.operator} • {alert.count}x Today</p>
                                    </div>
                                </div>
                                <div className={`text-[1rem] p-2 rounded-lg flex justify-between items-center ${darkMode ? 'bg-slate-900' : 'bg-slate-100'}`}>
                                    <div className={`flex items-center gap-2 font-medium ${darkMode ? 'text-slate-300' : 'text-slate-500'}`}><Map size={18} /><span>{alert.location}</span></div>
                                    <div className="flex items-center gap-2 text-red-400 font-mono font-bold animate-pulse"><Clock size={18} /><span>+{getOpenDuration(alert.time)}m</span></div>
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