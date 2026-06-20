// Minimal fetch-based API client for the Attendance console.
//
// Talks to the .NET API through the Vite /api proxy (see vite.config.js), so it's
// same-origin — no CORS, no cookies needed. The access token from /api/auth/login is
// kept in localStorage and sent as a Bearer header. Plain JSON in/out (the backend
// LoginRequestDTO etc. expect plain fields — we deliberately do NOT encrypt).

const TOKEN_KEY = 'att_token'
const USER_KEY = 'att_user'

export const auth = {
  get token() {
    return localStorage.getItem(TOKEN_KEY)
  },
  get user() {
    try {
      return JSON.parse(localStorage.getItem(USER_KEY) || 'null')
    } catch {
      return null
    }
  },
  set(token, user) {
    localStorage.setItem(TOKEN_KEY, token)
    localStorage.setItem(USER_KEY, JSON.stringify(user ?? null))
  },
  clear() {
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(USER_KEY)
  },
  // Admin + HR get the management views (team table, schedule editor, branch editor).
  // The "User" role is the basic employee who just clocks in.
  get isManager() {
    const role = auth.user?.roleName
    return role === 'Admin' || role === 'HR'
  },
  get isHR() {
    return auth.user?.roleName === 'HR'
  },
  get isAdmin() {
    return auth.user?.roleName === 'Admin'
  },
}

async function request(method, path, body) {
  let res
  try {
    res = await fetch(`/api${path}`, {
      method,
      headers: {
        'Content-Type': 'application/json',
        ...(auth.token ? { Authorization: `Bearer ${auth.token}` } : {}),
      },
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  } catch {
    return { isSuccess: false, message: 'Cannot reach the server. Is the API running?', data: null }
  }

  if (res.status === 401) {
    const wasLoggedIn = !!auth.token
    auth.clear()
    // Only treat as session expiry when the user was already authenticated.
    // Login failures (wrong password) also return 401 but should show the server's message.
    if (wasLoggedIn) {
      window.dispatchEvent(new Event('auth:expired'))
      return { isSuccess: false, message: 'Your session expired. Please sign in again.', data: null, status: 401 }
    }
    // Not logged in — fall through to read the actual error body from the server.
  }

  let payload = null
  try {
    payload = await res.json()
  } catch {
    payload = null
  }

  // The API wraps everything in ServiceResponse { isSuccess, message, data }.
  if (payload && typeof payload.isSuccess === 'boolean') return payload

  if (!res.ok) {
    return { isSuccess: false, message: payload?.message || `Request failed (${res.status})`, data: null }
  }
  return { isSuccess: true, data: payload }
}

export const api = {
  get: (p) => request('GET', p),
  post: (p, b) => request('POST', p, b ?? {}),
  put: (p, b) => request('PUT', p, b ?? {}),
  del: (p) => request('DELETE', p),
}

// ── Endpoint helpers ───────────────────────────────────────────────
export const authApi = {
  login: (username, password) => api.post('/auth/login', { username, password }),
}

export const scheduleApi = {
  mine: () => api.get('/schedule/my'),
  all: () => api.get('/schedule'),
  set: (dto) => api.post('/schedule', dto),
  remove: (employeeId) => api.del(`/schedule/employee/${employeeId}`),
}

export const attendanceApi = {
  today: () => api.get('/attendance/my-today'),
  history: (page = 1, size = 50) => api.get(`/attendance/my-history?pageNumber=${page}&pageSize=${size}`),
  adminAll: (startDate, endDate, page = 1, size = 200) =>
    api.get(`/attendance/admin/all?startDate=${startDate}&endDate=${endDate}&pageNumber=${page}&pageSize=${size}`),
  // coords = { latitude, longitude } | null — used for the Office/Outside geofence check.
  requestOtp: (coords) => api.post('/attendance/clock-in/request-otp', coords || {}),
  verify: (otpCode) => api.post('/attendance/clock-in/verify', { otpCode }),
  clockOut: () => api.post('/attendance/clock-out', {}),
  timeOff: () => api.post('/attendance/time-off', {}),
  byDate: (date) => api.get(`/attendance/admin/by-date/${date}`),
  employees: () => api.get('/employees?pageNumber=1&pageSize=200'),
  myCalendar: (year, month) => api.get(`/attendance/my-calendar?year=${year}&month=${month}`),
}

export const branchApi = {
  list: (activeOnly = false) => api.get(`/branch?activeOnly=${activeOnly}`),
  set: (dto) => api.post('/branch', dto),
  remove: (id) => api.del(`/branch/${id}`),
}

export const usersApi = {
  list: () => api.get('/users'),
  roles: () => api.get('/users/roles'),
  create: (dto) => api.post('/users', dto),
  update: (id, dto) => api.put(`/users/${id}`, dto),
}

export const leaveApi = {
  mine: () => api.get('/leave/mine'),
  submit: (dto) => api.post('/leave', dto),
  cancel: (id) => api.del(`/leave/${id}`),
  all: (status) => api.get(`/leave/all${status ? `?status=${status}` : ''}`),
  approve: (id, dto) => api.put(`/leave/${id}/approve`, dto),
}

export const workEventApi = {
  byMonth: (year, month) => api.get(`/work-events?year=${year}&month=${month}`),
  create: (dto) => api.post('/work-events', dto),
  remove: (id) => api.del(`/work-events/${id}`),
}
