/**
 * @typedef {Object} SummaryData
 * @property {number} totalAlarms
 * @property {number} followedUp
 * @property {number} waitingFollowUp
 */

/**
 * @typedef {Object} AreaStats
 * @property {number} total
 * @property {number} open
 * @property {number} resolved
 */

/**
 * @typedef {Object} AreaSummary
 * @property {AreaStats} mining
 * @property {AreaStats} hauling
 */

/**
 * @typedef {Object} AreaDistribution
 * @property {string} location
 * @property {number} count
 */

/**
 * @typedef {Object} ActiveAlert
 * @property {number} id
 * @property {string} externalId
 * @property {string} unitName
 * @property {string} operatorName
 * @property {string} alertType
 * @property {string} area
 * @property {string} location
 * @property {string} eventTime
 * @property {string} eventTimeFormatted
 * @property {number} openDurationMinutes
 * @property {string} status
 * @property {number} speed
 * @property {number} alertCountToday
 * @property {string|null} imageUrl
 * @property {string|null} videoUrl
 * @property {number} latitude
 * @property {number} longitude
 */

/**
 * @typedef {Object} DelayedFollowUp
 * @property {number} id
 * @property {string} externalId
 * @property {string} unitName
 * @property {string} operatorName
 * @property {string} area
 * @property {string} location
 * @property {string} eventTime
 * @property {number} delayMinutes
 */

/**
 * @typedef {Object} RecurrentUnit
 * @property {number} id
 * @property {string} unitName
 * @property {string} operatorName
 * @property {number} eventCount
 * @property {string} primaryArea
 * @property {string} status
 */

/**
 * @typedef {Object} HighRiskArea
 * @property {number} id
 * @property {string} location
 * @property {string} area
 * @property {number} eventCount
 * @property {string} riskLevel
 */

/**
 * @typedef {Object} DashboardData
 * @property {SummaryData} summary
 * @property {AreaSummary} areaSummary
 * @property {AreaDistribution[]} miningDistribution
 * @property {AreaDistribution[]} haulingDistribution
 * @property {ActiveAlert[]} activeAlerts
 * @property {DelayedFollowUp[]} delayedFollowUps
 * @property {RecurrentUnit[]} recurrentUnits
 * @property {HighRiskArea[]} highRiskAreas
 * @property {string} lastUpdated
 */

/**
 * @typedef {'connecting' | 'connected' | 'disconnected'} SseConnectionStatus
 */

export const SSE_STATUS = {
  CONNECTING: 'connecting',
  CONNECTED: 'connected',
  DISCONNECTED: 'disconnected'
};
