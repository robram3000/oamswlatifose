import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { attendanceApi, scheduleApi, auth, workEventApi, leaveApi } from '../../lib/api'
import { parseCsvImport, downloadImportTemplate } from '../../lib/export'
import { getCurrentLocation } from '../../lib/geo'
import { Icons, Sparkline, statusColor, statusBadge, locationBadge } from '../../lib/ui'
import MonitoringTable from './MonitoringTable'
import ScheduleEditor from './ScheduleEditor'
import BranchEditor from './BranchEditor'
import UserManager from './UserManager'
import OtpModal from './OtpModal'
import ConfirmDeleteModal from './ConfirmDeleteModal'
import AttendanceCalendar from './AttendanceCalendar'
import LeaveRequestView from './LeaveRequestView'

const RANGES = [
  { key: 'today', label: 'Today', days: 1 },
  { key: '7d', label: '7 days', days: 7 },
  { key: '30d', label: '30 days', days: 30 },
  { key: '90d', label: '90 days', days: 90 },
]

const localDateStr = (d = new Date()) => {
  const z = new Date(d.getTime() - d.getTimezoneOffset() * 60000)
  return z.toISOString().slice(0, 10)
}
const rangeStart = (days) => {
  const d = new Date()
  d.setDate(d.getDate() - (days - 1))
  return localDateStr(d)
}
const isPresent = (s) => /present|on time/i.test(s || '')
const isLate = (s) => /late/i.test(s || '')
const isAbsent = (s) => /absent/i.test(s || '')
const isTimeOffStatus = (s) => /time.?off/i.test(s || '')

export default function AttendanceConsole({ user, onSignOut }) {
  const isManager = auth.isManager
  const isHR = auth.isHR
  const [view, setView] = useState('monitoring')
  const [sidebarOpen, setSidebarOpen] = useState(false)

  // Employee-only data
  const [today, setToday] = useState(null)
  const [schedule, setSchedule] = useState(null)
  const [history, setHistory] = useState([])
  const [loading, setLoading] = useState(true)
  const [rangeKey, setRangeKey] = useState('30d')
  const [notice, setNotice] = useState(null)
  const [acting, setActing] = useState(false)
  const [locating, setLocating] = useState(false)
  const [isOnLeave, setIsOnLeave] = useState(false)
  const [todayLeave, setTodayLeave] = useState(null) // the active EMLeaveRequest covering today

  // OTP modal
  const [otpInfo, setOtpInfo] = useState(null)
  const lastCoords = useRef(null)

  // Admin-verify flow
  const [verifyPending, setVerifyPending] = useState(false) // employee waiting for admin
  const [pendingRequests, setPendingRequests] = useState([])
  const [pendingLoading, setPendingLoading] = useState(false)
  const [approvingId, setApprovingId] = useState(null)

  // Manager-only
  const [teamDate, setTeamDate] = useState(localDateStr())
  const [teamRows, setTeamRows] = useState([])
  const [teamLoading, setTeamLoading] = useState(false)
  const [schedulesByEmp, setSchedulesByEmp] = useState({})
  const [editSchedEmpId, setEditSchedEmpId] = useState(null)
  const [showSchedModal, setShowSchedModal] = useState(false)
  const [viewSched, setViewSched] = useState(null)
  const [deleteSchedTarget, setDeleteSchedTarget] = useState(null)

  // Admin attendance (all employees range view)
  const [adminAttRows, setAdminAttRows] = useState([])
  const [adminAttLoading, setAdminAttLoading] = useState(false)
  const [adminRangeKey, setAdminRangeKey] = useState('7d')

  // Import state (Admin only)
  const [importLoading, setImportLoading] = useState(false)
  const [importNotice, setImportNotice] = useState(null)

  // Work event banner — shows upcoming events for next 14 days
  const [bannerEvents, setBannerEvents] = useState([])
  const [bannerOpen, setBannerOpen] = useState(false)

  // Dashboard trend (7-day history grouped by date)
  const [dashTrend, setDashTrend] = useState([])
  const [dashTrendLoading, setDashTrendLoading] = useState(false)

  const range = RANGES.find((r) => r.key === rangeKey) || RANGES[2]
  const adminRange = RANGES.find((r) => r.key === adminRangeKey) || RANGES[1]

  const loadMine = useCallback(async () => {
    setLoading(true)
    const [t, h, s, lv] = await Promise.all([
      attendanceApi.today(),
      attendanceApi.history(1, 100),
      scheduleApi.mine(),
      leaveApi.mine(),
    ])
    const todayData = t.isSuccess ? t.data : null
    setToday(todayData)
    if (todayData?.timeIn || todayData?.timeInFormatted) setVerifyPending(false)
    setHistory(h.isSuccess ? (h.data?.items ?? []) : [])
    setSchedule(s.isSuccess ? s.data : null)

    // Check if today falls within any approved leave date range
    const todayISO = localDateStr()
    if (lv.isSuccess && Array.isArray(lv.data)) {
      const active = lv.data.find(
        (l) => l.status === 'Approved' && todayISO >= l.startDate && todayISO <= l.endDate,
      )
      setIsOnLeave(!!active)
      setTodayLeave(active || null)
    } else {
      setIsOnLeave(false)
      setTodayLeave(null)
    }

    setLoading(false)
  }, [])

  const loadTeam = useCallback(async (date) => {
    setTeamLoading(true)
    const [byDate, all] = await Promise.all([attendanceApi.byDate(date), scheduleApi.all()])
    setTeamRows(byDate.isSuccess ? (Array.isArray(byDate.data) ? byDate.data : []) : [])
    const map = {}
    if (all.isSuccess && Array.isArray(all.data)) all.data.forEach((sc) => { map[sc.employeeId] = sc })
    setSchedulesByEmp(map)
    setTeamLoading(false)
  }, [])

  const loadAdminAtt = useCallback(async (days) => {
    setAdminAttLoading(true)
    const start = rangeStart(days)
    const end = localDateStr()
    const res = await attendanceApi.adminAll(start, end)
    setAdminAttRows(res.isSuccess ? (res.data?.items ?? res.data ?? []) : [])
    setAdminAttLoading(false)
  }, [])

  const loadPending = useCallback(async () => {
    if (!isManager) return
    setPendingLoading(true)
    const res = await attendanceApi.pendingRequests()
    setPendingRequests(res.isSuccess ? (res.data ?? []) : [])
    setPendingLoading(false)
  }, [isManager])

  const loadDashTrend = useCallback(async () => {
    if (!isManager) return
    setDashTrendLoading(true)
    const res = await attendanceApi.adminAll(rangeStart(7), localDateStr())
    const rows = res.isSuccess ? (res.data?.items ?? res.data ?? []) : []
    const byDate = {}
    rows.forEach((r) => {
      const d = r.date || r.attendanceDate
      if (!d) return
      if (!byDate[d]) byDate[d] = { date: d, present: 0, late: 0, timeOff: 0 }
      if (isPresent(r.status)) byDate[d].present++
      else if (isLate(r.status)) byDate[d].late++
      else if (isTimeOffStatus(r.status)) byDate[d].timeOff++
    })
    setDashTrend(Object.values(byDate).sort((a, b) => a.date.localeCompare(b.date)))
    setDashTrendLoading(false)
  }, [isManager])

  // Load upcoming work events for the banner
  useEffect(() => {
    const load = async () => {
      const now = new Date()
      const y = now.getFullYear()
      const m = now.getMonth() + 1
      const todayS = localDateStr(now)
      const limitDate = new Date(now)
      limitDate.setDate(limitDate.getDate() + 14)
      const limitS = localDateStr(limitDate)

      const res1 = await workEventApi.byMonth(y, m)
      let all = res1.isSuccess ? (res1.data ?? []) : []

      // If today is in the last ~14 days of the month, also load next month
      if (now.getDate() > 17) {
        const nm = m === 12 ? 1 : m + 1
        const ny = m === 12 ? y + 1 : y
        const res2 = await workEventApi.byMonth(ny, nm)
        if (res2.isSuccess && res2.data) all = [...all, ...res2.data]
      }

      const upcoming = all
        .filter((e) => e.date >= todayS && e.date <= limitS)
        .sort((a, b) => a.date.localeCompare(b.date))

      setBannerEvents(upcoming)
      if (upcoming.length > 0) setBannerOpen(true)
    }
    load()
  }, [])

  const handleImport = useCallback(async (file) => {
    if (!file) return
    setImportLoading(true)
    setImportNotice(null)
    const text = await file.text()
    const records = parseCsvImport(text)
    if (!records.length) {
      setImportNotice({ type: 'error', text: 'No valid records found. Check EmployeeId and Date columns match the template.' })
      setImportLoading(false)
      return
    }
    const res = await attendanceApi.bulkImport(records)
    setImportLoading(false)
    if (res.isSuccess) {
      const d = res.data
      const msg = `Imported ${d?.successCount ?? records.length} of ${d?.totalRecords ?? records.length} records${d?.failCount ? ` (${d.failCount} failed)` : ''}.`
      setImportNotice({ type: 'ok', text: msg })
      loadAdminAtt(adminRange.days)
    } else {
      setImportNotice({ type: 'error', text: res.message || 'Import failed.' })
    }
  }, [loadAdminAtt, adminRange.days])

  useEffect(() => { loadMine() }, [loadMine])
  useEffect(() => { if (isManager) loadTeam(teamDate) }, [isManager, teamDate, loadTeam])
  useEffect(() => { if (isManager) loadPending() }, [isManager, loadPending])
  useEffect(() => {
    if (isManager && view === 'attendance') loadAdminAtt(adminRange.days)
  }, [isManager, view, adminRangeKey, loadAdminAtt, adminRange.days])
  useEffect(() => {
    if (isManager && view === 'monitoring') loadDashTrend()
  }, [isManager, view, loadDashTrend])

  // Tick every minute so time-based button states stay current
  const [nowMin, setNowMin] = useState(() => {
    const n = new Date(); return n.getHours() * 60 + n.getMinutes()
  })
  useEffect(() => {
    const id = setInterval(() => {
      const n = new Date(); setNowMin(n.getHours() * 60 + n.getMinutes())
    }, 30000)
    return () => clearInterval(id)
  }, [])

  // ── Derived: employee clock state ─────────────────────────────────
  const hasTimeIn = !!(today && (today.timeInFormatted || today.timeIn))
  const hasTimeOut = !!(today && (today.timeOutFormatted || today.timeOut))
  const isTimeOff = !!(today && isTimeOffStatus(today.status))

  // Time Off is only enabled once the shift end time has been reached.
  // If no schedule is assigned, allow it freely.
  const canTimeOff = useMemo(() => {
    if (!schedule?.endTime) return true
    const parts = schedule.endTime.split(':').map(Number)
    const endMin = parts[0] * 60 + (parts[1] || 0)
    return nowMin >= endMin
  }, [schedule, nowMin])

  // ── Derived: employee range-filtered history + metrics ─────────────
  const filtered = useMemo(() => {
    const cutoff = new Date()
    cutoff.setHours(0, 0, 0, 0)
    cutoff.setDate(cutoff.getDate() - (range.days - 1))
    return history.filter((r) => {
      const d = new Date(`${r.date}T00:00:00`)
      return !Number.isNaN(d.getTime()) && d >= cutoff
    })
  }, [history, range])

  const metrics = useMemo(() => {
    const present = filtered.filter((r) => isPresent(r.status)).length
    const late = filtered.filter((r) => isLate(r.status)).length
    const absent = filtered.filter((r) => isAbsent(r.status)).length
    const graded = present + late + absent
    const rate = graded ? Math.round((present / graded) * 100) : 0
    const chrono = [...filtered].reverse()
    return {
      present, late, absent, rate,
      presentSeries: chrono.map((r) => (isPresent(r.status) ? 1 : 0)),
      lateSeries: chrono.map((r) => (isLate(r.status) ? 1 : 0)),
      absentSeries: chrono.map((r) => (isAbsent(r.status) ? 1 : 0)),
      hoursSeries: chrono.map((r) => parseFloat(r.hoursWorked) || 0),
    }
  }, [filtered])

  // ── Derived: dashboard stats from today's team rows ────────────────
  const dashStats = useMemo(() => ({
    present: teamRows.filter((r) => isPresent(r.status)).length,
    late: teamRows.filter((r) => isLate(r.status)).length,
    timeOff: teamRows.filter((r) => isTimeOffStatus(r.status)).length,
    total: teamRows.length,
  }), [teamRows])

  // ── Actions ───────────────────────────────────────────────────────
  const requestOtp = useCallback(async () => {
    const res = await attendanceApi.requestOtp(lastCoords.current)
    if (res.isSuccess) setOtpInfo(res.data)
    return res
  }, [])

  const startTimeIn = async () => {
    setNotice(null)
    setLocating(true)
    lastCoords.current = await getCurrentLocation(3000) // 3 s max — location is optional
    setLocating(false)
    setActing(true)
    const res = await requestOtp()
    setActing(false)
    if (!res.isSuccess) setNotice({ type: 'error', text: res.message })
  }

  const verify = async (code) => {
    const res = await attendanceApi.verify(code)
    if (res.isSuccess) {
      setOtpInfo(null)
      setVerifyPending(false)
      setNotice({ type: 'ok', text: res.message || 'Clocked in.' })
      await loadMine()
      if (isManager) loadTeam(teamDate)
    }
    return res
  }

  const startRequestVerify = async () => {
    setNotice(null)
    setLocating(true)
    lastCoords.current = await getCurrentLocation(3000)
    setLocating(false)
    setActing(true)
    const res = await attendanceApi.requestVerify(lastCoords.current)
    setActing(false)
    if (res.isSuccess) {
      setVerifyPending(true)
      setNotice({ type: 'ok', text: `Verification request submitted at ${res.data?.requestedTimeFormatted || 'now'} — waiting for HR/Admin approval.` })
    } else {
      setNotice({ type: 'error', text: res.message })
    }
  }

  const approveRequest = async (requestId) => {
    setApprovingId(requestId)
    const res = await attendanceApi.approveRequest(requestId)
    setApprovingId(null)
    if (res.isSuccess) {
      setNotice({ type: 'ok', text: res.message || 'Clock-in approved.' })
      loadPending()
      loadTeam(teamDate)
    } else {
      setNotice({ type: 'error', text: res.message || 'Approval failed.' })
      loadPending()
    }
  }

  const clockOut = async () => {
    setNotice(null)
    setActing(true)
    const res = await attendanceApi.clockOut()
    setActing(false)
    setNotice({ type: res.isSuccess ? 'ok' : 'error', text: res.message || (res.isSuccess ? 'Clocked out.' : 'Clock-out failed.') })
    if (res.isSuccess) { await loadMine(); if (isManager) loadTeam(teamDate) }
  }

  const clockTimeOff = async () => {
    setNotice(null)
    setActing(true)
    const res = await attendanceApi.timeOff()
    setActing(false)
    setNotice({ type: res.isSuccess ? 'ok' : 'error', text: res.message || (res.isSuccess ? 'Time off marked.' : 'Failed to mark time off.') })
    if (res.isSuccess) { await loadMine(); if (isManager) loadTeam(teamDate) }
  }

  const refresh = () => {
    loadMine()
    if (isManager) {
      loadTeam(teamDate)
      loadPending()
      if (view === 'monitoring') loadDashTrend()
      if (view === 'attendance') loadAdminAtt(adminRange.days)
    }
  }

  const handleEditSchedule = (row) => {
    setEditSchedEmpId(row.employeeId)
    setShowSchedModal(true)
  }
  const closeSchedModal = () => { setShowSchedModal(false); setEditSchedEmpId(null) }

  const handleDeleteSchedule = (row) => setDeleteSchedTarget(row)

  const confirmDeleteSchedule = async () => {
    const res = await scheduleApi.remove(deleteSchedTarget.employeeId)
    setDeleteSchedTarget(null)
    setNotice({ type: res.isSuccess ? 'ok' : 'error', text: res.message || (res.isSuccess ? 'Schedule deleted.' : 'Delete failed.') })
    if (res.isSuccess) loadTeam(teamDate)
  }

  // ── Primary action button — hidden for Admin, shown for HR & User ──
  const ActionButton = ({ inHeader }) => {
    if (auth.isAdmin) return null
    if (isTimeOff) {
      return <button className="btnGhost" disabled>{Icons.umbrella} Time Off</button>
    }
    // On approved leave and not yet clocked in → block Time In and Time Off
    if (isOnLeave && !hasTimeIn) {
      const tip = todayLeave
        ? `${todayLeave.leaveType} leave · ${todayLeave.startDate} – ${todayLeave.endDate}`
        : undefined
      return (
        <button className="btnGhost" disabled title={tip}>
          {Icons.leave} On Approved Leave
        </button>
      )
    }
    if (!hasTimeIn) {
      const busy = acting || locating
      const endLabel = schedule?.endTime ? schedule.endTime.slice(0, 5) : null
      if (verifyPending) {
        return (
          <button className="btnGhost" disabled>
            <span className="spinner spinner--blue" /> Waiting for approval…
          </button>
        )
      }
      return (
        <div className="actionBtns">
          <button className="btnPrimary" onClick={startTimeIn} disabled={busy}>
            {busy ? <span className="spinner" /> : Icons.clock}
            {locating ? 'Locating…' : acting ? 'Sending code…' : 'Time In'}
          </button>
          <button className="btnGhost" onClick={startRequestVerify} disabled={busy} title="Ask HR/Admin to verify your clock-in instead of using an OTP code">
            {busy ? <span className="spinner" /> : Icons.check}
            {acting ? 'Submitting…' : 'Request Verify'}
          </button>
          <button
            className="btnGhost"
            onClick={clockTimeOff}
            disabled={busy || !canTimeOff}
            title={!canTimeOff ? `Available after shift ends${endLabel ? ` (${endLabel})` : ''}` : undefined}
          >
            {Icons.umbrella} Time Off
          </button>
        </div>
      )
    }
    if (!hasTimeOut) {
      return (
        <button className={inHeader ? 'btnGhost' : 'btnPrimary'} onClick={clockOut} disabled={acting}>
          {acting ? <span className="spinner spinner--blue" /> : Icons.clock}
          {acting ? 'Working…' : 'Time Out'}
        </button>
      )
    }
    return <button className="btnGhost" disabled>Done for today</button>
  }

  // My schedule panel — only for employees
  const mySchedulePanel = !isManager && (
    <div className="panel">
      <h3 className="panel__title">My schedule</h3>
      {schedule ? (
        <>
          <div className="kv"><span className="kv__k">Start time</span><span className="kv__v">{schedule.startTime}</span></div>
          <div className="kv"><span className="kv__k">End time</span><span className="kv__v">{schedule.endTime}</span></div>
          <div className="kv"><span className="kv__k">Late after</span><span className="kv__v">{schedule.lateAfter}</span></div>
          <div className="kv"><span className="kv__k">Grace</span><span className="kv__v">{schedule.graceMinutes} min</span></div>
          <div className="kv"><span className="kv__k">Work days</span><span className="kv__v">{schedule.workDays}</span></div>
        </>
      ) : (
        <p className="alert alert--info">No schedule set yet. Ask Admin/HR to set your schedule — clock-ins default to a 09:00 start until then.</p>
      )}
    </div>
  )

  const VIEWS = {
    monitoring: { title: isManager ? 'Dashboard' : 'Attendance monitoring', sub: isManager ? "Today's attendance overview." : 'Clock in against your schedule — verified by an emailed one-time code.' },
    attendance: { title: isManager ? 'All attendance' : 'My attendance', sub: isManager ? "Every employee's attendance records." : 'Your attendance history, location and on-time rate.' },
    calendar: { title: 'My calendar', sub: 'Monthly view of your attendance — present, absent, leave, weekly off and holidays.' },
    leave: { title: 'Leave requests', sub: isManager ? 'Review and approve employee leave requests.' : 'Request leave and view your leave history.' },
    events: { title: 'Work events', sub: 'Manage custom holidays, days off, and attendance open/close by date.' },
    schedule: { title: 'Schedule', sub: isManager ? 'Set work schedules per employee.' : 'Your assigned work schedule.' },
    users: { title: 'Users', sub: 'Create employee accounts and assign their role.' },
  }

  const navItem = (key, icon, label) => (
    <button className={`navItem ${view === key ? 'navItem--active' : ''}`} onClick={() => { setView(key); setSidebarOpen(false) }}>
      {icon} {label}
    </button>
  )

  const initials = (user?.employeeName || user?.username || '?').trim().slice(0, 1).toUpperCase()

  return (
    <div className="shell">
      {sidebarOpen && <div className="sidebarOverlay" onClick={() => setSidebarOpen(false)} />}

      <aside className={`sidebar${sidebarOpen ? ' sidebar--open' : ''}`}>
        <div className="sidebar__brand">
          <img src="/logo.svg" alt="AGLIPAY" className="brandLogo" />
          <div className="brandText">
            <span className="brandTitle">AGLIPAY</span>
            <span className="brandSub">Attendance monitoring</span>
          </div>
        </div>
        {navItem('monitoring', Icons.monitor, isManager ? 'Dashboard' : 'Monitoring')}
        {navItem('attendance', Icons.clock, isManager ? 'All attendance' : 'My attendance')}
        {!isManager && navItem('calendar', Icons.calendar, 'My calendar')}
        {navItem('leave', Icons.leave, 'Leave')}
        {isManager && navItem('events', Icons.events, 'Work events')}
        {navItem('schedule', Icons.calendar, 'Schedule')}
        {isManager && navItem('users', Icons.users, 'Users')}
      </aside>

      <div className="main">
        <header className="topbar">
          <button className="iconBtn menuBtn" onClick={() => setSidebarOpen(o => !o)} title="Menu">
            {sidebarOpen ? Icons.close : Icons.menu}
          </button>
          <span className="topbar__title">AGLIPAY · Attendance monitoring</span>
          <div className="topbar__spacer" />
          <div className="topbar__user">
            <span className="topbar__userName">{user?.employeeName || user?.username}{user?.roleName ? ` · ${user.roleName}` : ''}</span>
            <span className="avatar">{initials}</span>
            <button className="iconBtn" title="Sign out" onClick={onSignOut}>{Icons.logout}</button>
          </div>
        </header>

        <div className="page">
          <div className="topRow">
            <div>
              <h1 className="pageTitle">{VIEWS[view].title}</h1>
              <p className="pageSub">{VIEWS[view].sub}</p>
            </div>
            <div className="headerActions">
              <button className="iconBtn" onClick={refresh} title="Refresh">{Icons.refresh}</button>
              <ActionButton inHeader />
            </div>
          </div>

          {notice && (
            <p className={`alert ${notice.type === 'ok' ? 'alert--ok' : notice.type === 'error' ? 'alert--error' : 'alert--info'}`}>
              {notice.text}
            </p>
          )}

          {bannerOpen && bannerEvents.length > 0 && (
            <WorkEventBanner events={bannerEvents} onDismiss={() => setBannerOpen(false)} />
          )}

          {/* ===================== MONITORING / DASHBOARD ===================== */}
          {view === 'monitoring' && (
            <>
              {isManager ? (
                /* ─── Admin Dashboard ─── */
                <>
                  <div className="topRow" style={{ marginBottom: 8 }}>
                    <div>
                      <span className="pageSub">Showing: {teamDate}</span>
                    </div>
                    <div className="field">
                      <label htmlFor="td2">Date</label>
                      <input id="td2" type="date" className="input" value={teamDate} onChange={(e) => setTeamDate(e.target.value)} />
                    </div>
                  </div>

                  <div className="cardsGrid">
                    <MetricCard title="Present" value={dashStats.present} color="var(--gcp-green)" series={Array(7).fill(0).map((_, i) => i === 6 ? dashStats.present : 0)} />
                    <MetricCard title="Late" value={dashStats.late} color="var(--gcp-yellow)" series={Array(7).fill(0).map((_, i) => i === 6 ? dashStats.late : 0)} />
                    <MetricCard title="Time Off" value={dashStats.timeOff} color="var(--gcp-blue)" series={Array(7).fill(0).map((_, i) => i === 6 ? dashStats.timeOff : 0)} />
                    <MetricCard title="Total records" value={dashStats.total} color="var(--text-secondary)" series={[]} />
                  </div>

                  <div className="chartsRow">
                    <div className="panel" style={{ flex: '1 1 260px', minWidth: 220 }}>
                      <h3 className="panel__title">Today&apos;s breakdown</h3>
                      <DonutChart present={dashStats.present} late={dashStats.late} timeOff={dashStats.timeOff} />
                    </div>
                    <div className="panel" style={{ flex: '2 1 360px', minWidth: 280 }}>
                      <h3 className="panel__title">7-day trend</h3>
                      <TrendBar data={dashTrend} loading={dashTrendLoading} />
                    </div>
                  </div>

                  <MonitoringTable
                    loading={teamLoading}
                    emptyText="No attendance recorded for this date."
                    filterKeys={['employeeName', 'department', 'status']}
                    exportOptions={{ filename: `team_attendance_${teamDate}`, title: `Team Attendance · ${teamDate}` }}
                    rows={teamRows.map((r) => ({
                      ...r,
                      scheduled: schedulesByEmp[r.employeeId]
                        ? `${schedulesByEmp[r.employeeId].startTime}–${schedulesByEmp[r.employeeId].endTime}`
                        : '—',
                    }))}
                    columns={[
                      { key: 'employeeName', label: 'Employee', render: (r) => (
                        <><span className="statusDot" style={{ background: statusColor(r.status) }} />{r.employeeName}</>
                      ) },
                      { key: 'department', label: 'Dept', hideSm: true },
                      { key: 'scheduled', label: 'Schedule', hideSm: true },
                      { key: 'timeInFormatted', label: 'In', render: (r) => r.timeInFormatted || <span className="muted">—</span> },
                      { key: 'timeOutFormatted', label: 'Out', render: (r) => r.timeOutFormatted || <span className="muted">—</span> },
                      { key: 'status', label: 'Status', render: (r) => statusBadge(r.status) },
                      { key: 'workLocation', label: 'Location', hideSm: true, render: (r) => locationBadge(r.workLocation) },
                      { key: 'hoursWorkedFormatted', label: 'Hrs', num: true, render: (r) => r.hoursWorkedFormatted || <span className="muted">—</span> },
                    ]}
                  />

                  {/* ── Pending verification requests ── */}
                  <div className="panel" style={{ marginTop: 16 }}>
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 12 }}>
                      <div>
                        <h3 className="panel__title" style={{ margin: 0 }}>
                          Pending verifications
                          {pendingRequests.length > 0 && (
                            <span style={{ marginLeft: 8, padding: '2px 8px', borderRadius: 12, background: 'var(--gcp-red)', color: '#fff', fontSize: 11, fontWeight: 700 }}>
                              {pendingRequests.length}
                            </span>
                          )}
                        </h3>
                        <p className="pageSub" style={{ marginTop: 2 }}>Employees waiting for clock-in approval.</p>
                      </div>
                      <button className="iconBtn" onClick={loadPending} title="Refresh">{Icons.refresh}</button>
                    </div>
                    {pendingLoading ? (
                      <p className="muted" style={{ fontSize: 13 }}>Loading…</p>
                    ) : pendingRequests.length === 0 ? (
                      <p className="muted" style={{ fontSize: 13 }}>No pending requests.</p>
                    ) : (
                      <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                        {pendingRequests.map((req) => (
                          <div key={req.requestId} style={{
                            display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                            flexWrap: 'wrap', gap: 10, padding: '10px 14px',
                            border: '1px solid var(--border-color)', borderRadius: 8,
                            background: 'var(--surface)'
                          }}>
                            <div style={{ flex: 1, minWidth: 180 }}>
                              <div style={{ fontWeight: 600, fontSize: 14 }}>{req.employeeName}</div>
                              <div style={{ fontSize: 12, color: 'var(--text-secondary)', marginTop: 2 }}>
                                {req.department && <span style={{ marginRight: 8 }}>{req.department}</span>}
                                Requested at <strong>{req.requestedTimeFormatted}</strong>
                                {req.workLocation && <span style={{ marginLeft: 8 }}>{req.onSite ? `· ${req.branchName || 'Office'}` : `· ${req.workLocation}`}</span>}
                              </div>
                            </div>
                            <button
                              className="btnPrimary"
                              style={{ fontSize: 13, padding: '6px 16px' }}
                              onClick={() => approveRequest(req.requestId)}
                              disabled={approvingId === req.requestId}
                            >
                              {approvingId === req.requestId ? <><span className="spinner" /> Approving…</> : `${Icons.check} Approve`}
                            </button>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>

                  <BranchEditor onChanged={() => loadTeam(teamDate)} />

                  {/* HR users can also clock in/out for themselves */}
                  {isHR && (
                    <div className="panel" style={{ marginTop: 16 }}>
                      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 12 }}>
                        <div>
                          <h3 className="panel__title" style={{ margin: 0 }}>My attendance today</h3>
                          <div style={{ fontSize: 13, color: 'var(--text-secondary)', marginTop: 4 }}>
                            {isTimeOff ? 'Time Off'
                              : isOnLeave && !hasTimeIn ? `On Approved Leave${todayLeave ? ` · ${todayLeave.leaveType}` : ''}`
                              : !hasTimeIn ? 'Not clocked in'
                              : hasTimeOut ? `Done · In ${today?.timeInFormatted} · Out ${today?.timeOutFormatted}`
                              : `Clocked in at ${today?.timeInFormatted}`}
                          </div>
                        </div>
                        <div className="actions"><ActionButton /></div>
                      </div>
                    </div>
                  )}
                </>
              ) : (
                /* ─── Employee monitoring ─── */
                <>
                  <div className="split">
                    <div className="panel">
                      <h3 className="panel__title">Today · {localDateStr()}</h3>
                      <div className="statusBig">
                        <span className="statusBig__dot" style={{ background: isOnLeave && !hasTimeIn ? 'var(--gcp-blue)' : hasTimeIn ? statusColor(today?.status) : 'var(--text-disabled)' }} />
                        <div>
                          <div className="statusBig__label">
                            {isTimeOff ? 'Time Off'
                              : isOnLeave && !hasTimeIn ? 'On Approved Leave'
                              : !hasTimeIn ? 'Not clocked in'
                              : hasTimeOut ? 'Completed'
                              : (today?.status || 'Clocked in')}
                          </div>
                          <div className="statusBig__sub">
                            {isOnLeave && !hasTimeIn && todayLeave
                              ? `${todayLeave.leaveType} leave · ${todayLeave.startDate} – ${todayLeave.endDate}${todayLeave.reason ? ` · ${todayLeave.reason}` : ''}`
                              : hasTimeIn
                              ? `In at ${today?.timeInFormatted || '—'}${hasTimeOut ? ` · Out at ${today?.timeOutFormatted}` : ''}${today?.workLocation ? ` · ${today.workLocation}` : ''}`
                              : schedule ? `Scheduled ${schedule.startTime} — late after ${schedule.lateAfter}` : 'No schedule set'}
                          </div>
                        </div>
                      </div>
                      <div className="actions"><ActionButton /></div>
                    </div>
                    {mySchedulePanel}
                  </div>
                </>
              )}
            </>
          )}

          {/* ===================== ATTENDANCE ===================== */}
          {view === 'attendance' && (
            <>
              {isManager ? (
                /* ─── Admin: all employees' attendance ─── */
                <>
                  <div className="rangeRow">
                    {RANGES.map((r) => (
                      <button key={r.key} className={`chip ${adminRangeKey === r.key ? 'chip--active' : ''}`} onClick={() => setAdminRangeKey(r.key)}>
                        {adminRangeKey === r.key && '✓ '}{r.label}
                      </button>
                    ))}
                    <div style={{ flex: 1 }} />
                    <label className="chip" style={{ cursor: 'pointer' }} title="Import attendance from CSV file">
                      {importLoading ? 'Importing…' : '↑ Import CSV'}
                      <input type="file" accept=".csv,.CSV" hidden disabled={importLoading}
                        onChange={(e) => { handleImport(e.target.files[0]); e.target.value = '' }} />
                    </label>
                    <button className="chip" onClick={downloadImportTemplate} title="Download CSV import template">Template</button>
                  </div>
                  {importNotice && (
                    <p className={`alert alert--${importNotice.type === 'ok' ? 'ok' : 'error'}`} style={{ margin: '8px 0' }}>
                      {importNotice.text}
                    </p>
                  )}
                  <MonitoringTable
                    loading={adminAttLoading}
                    emptyText="No attendance records in this range."
                    filterKeys={['employeeName', 'department', 'status', 'date']}
                    exportOptions={{ filename: `attendance_${adminRange.label.replace(' ', '_')}`, title: `All Attendance · ${adminRange.label}` }}
                    rows={Array.isArray(adminAttRows) ? adminAttRows : []}
                    columns={[
                      { key: 'employeeName', label: 'Employee' },
                      { key: 'department', label: 'Dept', hideSm: true },
                      { key: 'date', label: 'Date' },
                      { key: 'timeInFormatted', label: 'In', render: (r) => r.timeInFormatted || r.timeIn || <span className="muted">—</span> },
                      { key: 'timeOutFormatted', label: 'Out', render: (r) => r.timeOutFormatted || r.timeOut || <span className="muted">—</span> },
                      { key: 'status', label: 'Status', render: (r) => statusBadge(r.status) },
                      { key: 'workLocation', label: 'Location', hideSm: true, render: (r) => locationBadge(r.workLocation) },
                      { key: 'hoursWorked', label: 'Hrs', num: true, render: (r) => r.hoursWorkedFormatted || r.hoursWorked || <span className="muted">—</span> },
                    ]}
                  />
                </>
              ) : (
                /* ─── Employee: own attendance ─── */
                <>
                  <div className="rangeRow">
                    {RANGES.map((r) => (
                      <button key={r.key} className={`chip ${rangeKey === r.key ? 'chip--active' : ''}`} onClick={() => setRangeKey(r.key)}>
                        {rangeKey === r.key && '✓ '}{r.label}
                      </button>
                    ))}
                  </div>
                  <div className="cardsGrid">
                    <MetricCard title="Present" value={metrics.present} color="var(--gcp-green)" series={metrics.presentSeries} />
                    <MetricCard title="Late" value={metrics.late} color="var(--gcp-yellow)" series={metrics.lateSeries} />
                    <MetricCard title="Absent" value={metrics.absent} color="var(--gcp-red)" series={metrics.absentSeries} />
                    <MetricCard title="On-time rate" value={`${metrics.rate}%`} color="var(--gcp-blue)" series={metrics.hoursSeries} />
                  </div>
                  <MonitoringTable
                    loading={loading}
                    emptyText="No attendance records in this range yet."
                    filterKeys={['date', 'status']}
                    exportOptions={{ filename: `my_attendance_${range.label.replace(' ', '_')}`, title: `My Attendance · ${range.label}` }}
                    rows={filtered}
                    columns={[
                      { key: 'date', label: 'Date' },
                      { key: 'timeIn', label: 'In' },
                      { key: 'timeOut', label: 'Out' },
                      { key: 'status', label: 'Status', render: (r) => statusBadge(r.status) },
                      { key: 'workLocation', label: 'Location', hideSm: true, render: (r) => locationBadge(r.workLocation) },
                      { key: 'hoursWorked', label: 'Hrs', num: true },
                    ]}
                  />
                </>
              )}
            </>
          )}

          {/* ===================== CALENDAR ===================== */}
          {view === 'calendar' && !isManager && (
            <AttendanceCalendar schedule={schedule} />
          )}

          {/* ===================== LEAVE ===================== */}
          {view === 'leave' && <LeaveRequestView />}

          {/* ===================== WORK EVENTS ===================== */}
          {view === 'events' && isManager && <WorkEventPanel />}

          {/* ===================== SCHEDULE ===================== */}
          {view === 'schedule' && (
            <>
              {/* My schedule panel — employees only */}
              {mySchedulePanel}

              {isManager ? (
                <>
                  <div className="topRow" style={{ marginTop: 8 }}>
                    <div>
                      <h1 className="pageTitle" style={{ fontSize: 18 }}>All schedules</h1>
                      <p className="pageSub">Every employee's active work schedule.</p>
                    </div>
                    <button className="btnPrimary" onClick={() => { setEditSchedEmpId(null); setShowSchedModal(true) }}>
                      + Set schedule
                    </button>
                  </div>
                  <MonitoringTable
                    loading={teamLoading}
                    emptyText="No schedules set yet."
                    filterKeys={['employeeName']}
                    rows={Object.values(schedulesByEmp)}
                    columns={[
                      { key: 'employeeName', label: 'Employee' },
                      { key: 'startTime', label: 'Start' },
                      { key: 'endTime', label: 'End' },
                      { key: 'lateAfter', label: 'Late after', hideSm: true },
                      { key: 'graceMinutes', label: 'Grace (min)', num: true, hideSm: true },
                      { key: 'workDays', label: 'Work days', hideSm: true },
                      { key: '_actions', label: '', render: (r) => (
                        <div className="actionBtns">
                          <button className="btnSm" onClick={() => setViewSched(r)}>View</button>
                          <button className="btnSm" onClick={() => handleEditSchedule(r)}>Edit</button>
                          <button className="btnSm btnSm--danger" onClick={() => handleDeleteSchedule(r)}>Delete</button>
                        </div>
                      ) },
                    ]}
                  />
                </>
              ) : (
                <p className="alert alert--info">Your schedule is managed by Admin/HR. Contact them to change it.</p>
              )}
            </>
          )}

          {/* ===================== USERS ===================== */}
          {view === 'users' && isManager && <UserManager />}
        </div>
      </div>

      {/* OTP modal */}
      {otpInfo && (
        <OtpModal info={otpInfo} onVerify={verify} onResend={requestOtp} onClose={() => setOtpInfo(null)} />
      )}

      {/* Delete schedule confirmation modal */}
      {deleteSchedTarget && (
        <ConfirmDeleteModal
          title="Delete schedule"
          description={`This will permanently remove the work schedule for ${deleteSchedTarget.employeeName}. They will default to a 09:00 start until a new schedule is set.`}
          confirmText={deleteSchedTarget.employeeName}
          onConfirm={confirmDeleteSchedule}
          onClose={() => setDeleteSchedTarget(null)}
        />
      )}

      {/* Schedule editor modal */}
      {showSchedModal && (
        <div className="modalOverlay" onClick={closeSchedModal}>
          <div className="modal modal--wide" onClick={(e) => e.stopPropagation()}>
            <div className="modal__header">
              <h3 className="modal__title" style={{ margin: 0 }}>Set work schedule</h3>
              <button className="iconBtn" onClick={closeSchedModal}>{Icons.close}</button>
            </div>
            <ScheduleEditor
              schedulesByEmp={schedulesByEmp}
              onSaved={() => { loadTeam(teamDate); loadMine(); closeSchedModal() }}
              prefillEmployeeId={editSchedEmpId}
              hideTitle
            />
          </div>
        </div>
      )}

      {/* View schedule modal */}
      {viewSched && (
        <div className="modalOverlay" onClick={() => setViewSched(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h3 className="modal__title">{viewSched.employeeName}</h3>
            <p className="modal__sub">Work schedule details</p>
            <div className="kv"><span className="kv__k">Start time</span><span className="kv__v">{viewSched.startTime}</span></div>
            <div className="kv"><span className="kv__k">End time</span><span className="kv__v">{viewSched.endTime}</span></div>
            <div className="kv"><span className="kv__k">Late after</span><span className="kv__v">{viewSched.lateAfter}</span></div>
            <div className="kv"><span className="kv__k">Grace period</span><span className="kv__v">{viewSched.graceMinutes} min</span></div>
            <div className="kv"><span className="kv__k">Work days</span><span className="kv__v">{viewSched.workDays}</span></div>
            <div className="modal__actions">
              <button className="btnGhost" onClick={() => setViewSched(null)}>Close</button>
              <button className="btnPrimary" onClick={() => { handleEditSchedule(viewSched); setViewSched(null) }}>Edit</button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

function MetricCard({ title, value, color, series }) {
  const hasData = series && series.some((v) => v > 0)
  return (
    <div className="card">
      <div className="cardHead"><span className="cardTitle">{title}</span></div>
      <div className="cardValueRow"><span className="cardValue" style={{ color }}>{value}</span></div>
      <div className="cardSpark">
        {hasData ? <Sparkline data={series} color={color} /> : <span className="muted" style={{ fontSize: 12 }}>No data</span>}
      </div>
    </div>
  )
}

function DonutChart({ present, late, timeOff }) {
  const total = present + late + timeOff
  if (!total) return <p className="muted" style={{ textAlign: 'center', margin: '24px 0', fontSize: 13 }}>No attendance recorded today</p>

  const r = 34, cx = 50, cy = 50
  const C = 2 * Math.PI * r
  const segs = [
    { value: present, color: 'var(--gcp-green)', label: 'Present' },
    { value: late, color: 'var(--gcp-yellow)', label: 'Late' },
    { value: timeOff, color: 'var(--gcp-blue)', label: 'Time Off' },
  ].filter((s) => s.value > 0)

  let acc = 0
  const slices = segs.map((s) => {
    const len = (s.value / total) * C
    const offset = acc
    acc += len
    return { ...s, len, offset }
  })

  return (
    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 20, flexWrap: 'wrap', padding: '8px 0' }}>
      <svg viewBox="0 0 100 100" width={110} height={110} style={{ flexShrink: 0 }}>
        <g transform="rotate(-90 50 50)">
          {slices.map((s) => (
            <circle key={s.label} r={r} cx={cx} cy={cy}
              fill="none"
              stroke={s.color}
              strokeWidth={20}
              strokeDasharray={`${s.len} ${C - s.len}`}
              strokeDashoffset={-s.offset}
            />
          ))}
        </g>
        <text x="50" y="46" textAnchor="middle" fontSize="18" fontWeight="700" fill="var(--text-primary)">{total}</text>
        <text x="50" y="60" textAnchor="middle" fontSize="9" fill="var(--text-secondary)">total</text>
      </svg>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
        {segs.map((s) => (
          <div key={s.label} style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 13 }}>
            <span style={{ width: 10, height: 10, borderRadius: '50%', background: s.color, flexShrink: 0 }} />
            <span style={{ color: 'var(--text-secondary)', minWidth: 56 }}>{s.label}</span>
            <strong>{s.value}</strong>
            <span className="muted" style={{ fontSize: 11 }}>({Math.round(s.value / total * 100)}%)</span>
          </div>
        ))}
      </div>
    </div>
  )
}

function TrendBar({ data, loading }) {
  if (loading) return <p className="muted" style={{ textAlign: 'center', padding: 24, fontSize: 13 }}>Loading...</p>
  if (!data.length) return <p className="muted" style={{ textAlign: 'center', margin: '24px 0', fontSize: 13 }}>No data yet</p>

  const W = 460, H = 130
  const PL = 28, PR = 8, PT = 8, PB = 32
  const chartW = W - PL - PR
  const chartH = H - PT - PB
  const maxTotal = Math.max(...data.map((d) => d.present + d.late + d.timeOff), 1)
  const colW = chartW / data.length
  const barW = Math.max(colW * 0.55, 4)

  const fmt = (dateStr) => {
    const parts = dateStr.split('-')
    return `${parts[1]}/${parts[2]}`
  }

  return (
    <div>
      <svg viewBox={`0 0 ${W} ${H}`} width="100%" preserveAspectRatio="xMidYMid meet">
        {[0, 0.5, 1].map((frac) => {
          const y = PT + chartH * (1 - frac)
          return (
            <g key={frac}>
              <line x1={PL} x2={W - PR} y1={y} y2={y} stroke="var(--border-color)" strokeDasharray="2 4" opacity="0.5" />
              <text x={PL - 4} y={y} textAnchor="end" fontSize="8" dy="0.35em" fill="var(--text-secondary)">
                {Math.round(frac * maxTotal)}
              </text>
            </g>
          )
        })}

        {data.map((d, i) => {
          const x = PL + i * colW + colW / 2 - barW / 2
          const base = PT + chartH
          const scaleH = (v) => (v / maxTotal) * chartH
          const pH = scaleH(d.present)
          const lH = scaleH(d.late)
          const tH = scaleH(d.timeOff)
          return (
            <g key={d.date}>
              {d.present > 0 && <rect x={x} y={base - pH} width={barW} height={pH} fill="var(--gcp-green)" rx={2} />}
              {d.late > 0 && <rect x={x} y={base - pH - lH} width={barW} height={lH} fill="var(--gcp-yellow)" rx={2} />}
              {d.timeOff > 0 && <rect x={x} y={base - pH - lH - tH} width={barW} height={tH} fill="var(--gcp-blue)" rx={2} />}
              <text x={x + barW / 2} y={base + 14} textAnchor="middle" fontSize="9" fill="var(--text-secondary)">{fmt(d.date)}</text>
            </g>
          )
        })}
      </svg>
      <div style={{ display: 'flex', justifyContent: 'center', gap: 16, fontSize: 11, color: 'var(--text-secondary)', marginTop: 4 }}>
        {[['var(--gcp-green)', 'Present'], ['var(--gcp-yellow)', 'Late'], ['var(--gcp-blue)', 'Time Off']].map(([c, l]) => (
          <span key={l} style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
            <span style={{ width: 8, height: 8, borderRadius: 2, background: c, display: 'inline-block' }} />{l}
          </span>
        ))}
      </div>
    </div>
  )
}

const WE_CHIP = {
  Holiday: { color: '#9334e6', bg: 'rgba(147,52,230,.12)' },
  DayOff:  { color: 'var(--text-muted)', bg: 'rgba(120,120,120,.10)' },
  Closed:  { color: 'var(--gcp-red)', bg: 'rgba(234,67,53,.12)' },
}

function WorkEventBanner({ events, onDismiss }) {
  const today = localDateStr()
  const tomorrow = (() => { const d = new Date(); d.setDate(d.getDate() + 1); return localDateStr(d) })()

  const dayLabel = (dateStr) => {
    if (dateStr === today) return 'Today'
    if (dateStr === tomorrow) return 'Tomorrow'
    return new Date(dateStr + 'T12:00:00').toLocaleDateString('en-PH', { weekday: 'short', month: 'short', day: 'numeric' })
  }

  const todayEvents = events.filter((e) => e.date === today)
  const hasClosedToday = todayEvents.some((e) => e.eventType === 'Closed')

  return (
    <div
      className="panel"
      style={{
        marginBottom: 16,
        borderLeft: `3px solid ${hasClosedToday ? 'var(--gcp-red)' : 'var(--gcp-blue)'}`,
        padding: '12px 16px',
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 10 }}>
        <span style={{ fontWeight: 600, fontSize: 13, color: 'var(--text-primary)' }}>
          {Icons.events}&nbsp; Upcoming work events
        </span>
        <button className="iconBtn" onClick={onDismiss} title="Dismiss">{Icons.close}</button>
      </div>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 7 }}>
        {events.map((e) => {
          const s = WE_CHIP[e.eventType] || WE_CHIP.DayOff
          return (
            <div key={e.id} style={{ display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 8, fontSize: 13 }}>
              <span style={{ minWidth: 80, color: 'var(--text-muted)', fontSize: 12, flexShrink: 0 }}>
                {dayLabel(e.date)}
              </span>
              <span style={{ padding: '2px 9px', borderRadius: 12, background: s.bg, color: s.color, fontSize: 11, fontWeight: 600, flexShrink: 0 }}>
                {e.eventType}
              </span>
              <span style={{ fontWeight: 500 }}>{e.name}</span>
              {e.eventType === 'Closed' && (
                <span style={{ fontSize: 11, color: 'var(--gcp-red)', opacity: 0.8 }}>— clock-ins blocked</span>
              )}
            </div>
          )
        })}
      </div>
    </div>
  )
}

const EVENT_TYPES = ['Holiday', 'DayOff', 'Closed']
const EVENT_META = {
  Holiday: { color: '#9334e6', bg: 'rgba(147,52,230,.12)', label: 'Holiday',  desc: 'Company / public holiday — no attendance required.' },
  DayOff:  { color: 'var(--text-muted)', bg: 'rgba(120,120,120,.10)', label: 'Day Off', desc: 'No attendance expected for this day.' },
  Closed:  { color: 'var(--gcp-red)',  bg: 'rgba(234,67,53,.12)',  label: 'Closed',  desc: 'Clock-ins are blocked — employees cannot Time In.' },
}

const EMPTY_FORM = (now) => ({ date: now.toISOString().slice(0, 10), eventType: 'Holiday', name: '' })

function WorkEventPanel() {
  const now = new Date()
  const [year, setYear] = useState(now.getFullYear())
  const [month, setMonth] = useState(now.getMonth() + 1)
  const [events, setEvents] = useState([])
  const [loading, setLoading] = useState(true)
  const [notice, setNotice] = useState(null)

  // Add modal
  const [showAdd, setShowAdd] = useState(false)
  const [form, setForm] = useState(EMPTY_FORM(now))
  const [saving, setSaving] = useState(false)
  const [formErr, setFormErr] = useState(null)

  // Detail / delete modal
  const [viewEvent, setViewEvent] = useState(null)
  const [deleting, setDeleting] = useState(false)

  const MONTHS = ['January','February','March','April','May','June','July','August','September','October','November','December']

  const load = useCallback(async () => {
    setLoading(true)
    const res = await workEventApi.byMonth(year, month)
    setEvents(res.isSuccess ? (res.data ?? []) : [])
    setLoading(false)
  }, [year, month])

  useEffect(() => { load() }, [load])

  const openAdd = () => { setForm(EMPTY_FORM(new Date())); setFormErr(null); setShowAdd(true) }
  const closeAdd = () => { setShowAdd(false); setFormErr(null) }

  const addEvent = async () => {
    if (!form.name.trim()) { setFormErr('Name / description is required.'); return }
    setSaving(true)
    const res = await workEventApi.create({ date: form.date, eventType: form.eventType, name: form.name.trim() })
    setSaving(false)
    if (res.isSuccess) {
      closeAdd()
      setNotice({ type: 'ok', text: `"${form.name.trim()}" added.` })
      load()
    } else {
      setFormErr(res.message || 'Failed to add event.')
    }
  }

  const removeEvent = async (id) => {
    setDeleting(true)
    const res = await workEventApi.remove(id)
    setDeleting(false)
    if (res.isSuccess) { setViewEvent(null); load() }
    else setNotice({ type: 'error', text: res.message || 'Delete failed.' })
  }

  const prevMonth = () => { if (month === 1) { setYear(y => y - 1); setMonth(12) } else setMonth(m => m - 1) }
  const nextMonth = () => { if (month === 12) { setYear(y => y + 1); setMonth(1) } else setMonth(m => m + 1) }

  return (
    <div>
      {notice && (
        <p className={`alert alert--${notice.type === 'ok' ? 'ok' : 'error'}`} style={{ marginBottom: 16 }}>
          {notice.text}
        </p>
      )}

      <div className="panel">
        <div className="topRow" style={{ marginBottom: 14 }}>
          <div>
            <h3 className="panel__title" style={{ margin: 0 }}>Work events</h3>
            <p className="pageSub" style={{ marginTop: 2 }}>Holidays, days off, and attendance closures by date.</p>
          </div>
          <button className="btnPrimary" onClick={openAdd}>{Icons.plus} Add event</button>
        </div>

        {/* Month navigator */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 14 }}>
          <button className="iconBtn" onClick={prevMonth}>{Icons.chevLeft}</button>
          <span style={{ fontWeight: 600, minWidth: 130, textAlign: 'center' }}>{MONTHS[month - 1]} {year}</span>
          <button className="iconBtn" onClick={nextMonth}>{Icons.chevRight}</button>
        </div>

        {loading ? (
          <p className="muted" style={{ padding: '12px 0' }}>Loading…</p>
        ) : events.length === 0 ? (
          <p className="muted" style={{ fontSize: 13, padding: '12px 0' }}>No work events this month. Click <strong>Add event</strong> to create one.</p>
        ) : (
          events.map(ev => {
            const meta = EVENT_META[ev.eventType] || EVENT_META.DayOff
            return (
              <button
                key={ev.id}
                onClick={() => setViewEvent(ev)}
                style={{
                  display: 'flex', alignItems: 'center', width: '100%', textAlign: 'left',
                  padding: '10px 0', borderTop: 'none', borderLeft: 'none', borderRight: 'none',
                  borderBottom: '1px solid var(--border-color)',
                  background: 'none', cursor: 'pointer', gap: 12,
                }}
              >
                <span style={{ minWidth: 90, fontSize: 12, color: 'var(--text-muted)', flexShrink: 0 }}>{ev.date}</span>
                <span style={{ padding: '2px 9px', borderRadius: 12, background: meta.bg, color: meta.color, fontSize: 11, fontWeight: 600, flexShrink: 0 }}>
                  {meta.label}
                </span>
                <span style={{ fontSize: 14, fontWeight: 500, flex: 1, color: 'var(--text-primary)' }}>{ev.name}</span>
                <span style={{ color: 'var(--text-muted)', fontSize: 12 }}>{Icons.chevRight}</span>
              </button>
            )
          })
        )}
      </div>

      {/* ── Add event modal ─────────────────────────────────────── */}
      {showAdd && (
        <div className="modalOverlay" onClick={closeAdd}>
          <div className="modal modal--wide" onClick={e => e.stopPropagation()}>
            <div className="modal__header">
              <h3 className="modal__title" style={{ margin: 0 }}>Add work event</h3>
              <button className="iconBtn" onClick={closeAdd}>{Icons.close}</button>
            </div>
            <div style={{ padding: '0 24px 24px' }}>
              {formErr && <p className="alert alert--error" style={{ margin: '12px 0' }}>{formErr}</p>}

              <div className="fieldRow" style={{ marginTop: 16, flexWrap: 'wrap' }}>
                <div className="field">
                  <label>Date *</label>
                  <input type="date" className="input" value={form.date}
                    onChange={e => setForm(f => ({ ...f, date: e.target.value }))} />
                </div>
                <div className="field">
                  <label>Type *</label>
                  <select className="select" value={form.eventType}
                    onChange={e => setForm(f => ({ ...f, eventType: e.target.value }))}>
                    {EVENT_TYPES.map(t => <option key={t} value={t}>{t}</option>)}
                  </select>
                </div>
              </div>

              {/* Type hint */}
              {form.eventType && EVENT_META[form.eventType] && (
                <p className="muted" style={{ fontSize: 12, margin: '4px 0 12px' }}>
                  {EVENT_META[form.eventType].desc}
                </p>
              )}

              <div className="field" style={{ marginBottom: 0 }}>
                <label>Name / Description *</label>
                <input
                  className="input"
                  value={form.name}
                  onChange={e => { setForm(f => ({ ...f, name: e.target.value })); setFormErr(null) }}
                  placeholder={form.eventType === 'Holiday' ? 'e.g. Rizal Day' : form.eventType === 'DayOff' ? 'e.g. Team Building' : 'e.g. Office Maintenance'}
                  onKeyDown={e => e.key === 'Enter' && addEvent()}
                  autoFocus
                />
              </div>

              <div className="modal__actions">
                <button className="btnGhost" onClick={closeAdd}>Cancel</button>
                <button className="btnPrimary" onClick={addEvent} disabled={saving}>
                  {saving ? 'Adding…' : 'Add event'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ── Event detail modal ──────────────────────────────────── */}
      {viewEvent && (
        <div className="modalOverlay" onClick={() => !deleting && setViewEvent(null)}>
          <div className="modal" onClick={e => e.stopPropagation()} style={{ maxWidth: 380 }}>
            <div className="modal__header">
              <h3 className="modal__title" style={{ margin: 0 }}>Work event</h3>
              <button className="iconBtn" onClick={() => setViewEvent(null)}>{Icons.close}</button>
            </div>
            <div style={{ padding: '0 24px 24px' }}>
              {(() => {
                const meta = EVENT_META[viewEvent.eventType] || EVENT_META.DayOff
                return (
                  <>
                    <div style={{ padding: '14px 0', borderBottom: '1px solid var(--border-color)', marginBottom: 14 }}>
                      <span style={{ padding: '3px 12px', borderRadius: 14, background: meta.bg, color: meta.color, fontSize: 12, fontWeight: 600 }}>
                        {meta.label}
                      </span>
                    </div>
                    <div className="kv"><span className="kv__k">Date</span>
                      <span className="kv__v">{new Date(viewEvent.date + 'T12:00:00').toLocaleDateString('en-PH', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}</span>
                    </div>
                    <div className="kv"><span className="kv__k">Name</span><span className="kv__v">{viewEvent.name}</span></div>
                    <p className="muted" style={{ fontSize: 12, marginTop: 10 }}>{meta.desc}</p>
                  </>
                )
              })()}
              <div className="modal__actions">
                <button className="btnGhost" onClick={() => setViewEvent(null)}>Close</button>
                <button className="btnSm btnSm--danger" onClick={() => removeEvent(viewEvent.id)} disabled={deleting}>
                  {deleting ? 'Deleting…' : `${Icons.trash} Delete`}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

