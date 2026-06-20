import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { attendanceApi, scheduleApi, auth } from '../../lib/api'
import { getCurrentLocation } from '../../lib/geo'
import { Icons, Sparkline, statusColor, statusBadge, locationBadge } from '../../lib/ui'
import MonitoringTable from './MonitoringTable'
import ScheduleEditor from './ScheduleEditor'
import BranchEditor from './BranchEditor'
import UserManager from './UserManager'
import OtpModal from './OtpModal'

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

  // OTP modal
  const [otpInfo, setOtpInfo] = useState(null)
  const lastCoords = useRef(null)

  // Manager-only
  const [teamDate, setTeamDate] = useState(localDateStr())
  const [teamRows, setTeamRows] = useState([])
  const [teamLoading, setTeamLoading] = useState(false)
  const [schedulesByEmp, setSchedulesByEmp] = useState({})
  const [editSchedEmpId, setEditSchedEmpId] = useState(null)
  const [showSchedModal, setShowSchedModal] = useState(false)
  const [viewSched, setViewSched] = useState(null)

  // Admin attendance (all employees range view)
  const [adminAttRows, setAdminAttRows] = useState([])
  const [adminAttLoading, setAdminAttLoading] = useState(false)
  const [adminRangeKey, setAdminRangeKey] = useState('7d')

  const range = RANGES.find((r) => r.key === rangeKey) || RANGES[2]
  const adminRange = RANGES.find((r) => r.key === adminRangeKey) || RANGES[1]

  const loadMine = useCallback(async () => {
    setLoading(true)
    const [t, h, s] = await Promise.all([
      attendanceApi.today(),
      attendanceApi.history(1, 100),
      scheduleApi.mine(),
    ])
    setToday(t.isSuccess ? t.data : null)
    setHistory(h.isSuccess ? (h.data?.items ?? []) : [])
    setSchedule(s.isSuccess ? s.data : null)
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

  useEffect(() => { loadMine() }, [loadMine])
  useEffect(() => { if (isManager) loadTeam(teamDate) }, [isManager, teamDate, loadTeam])
  useEffect(() => {
    if (isManager && view === 'attendance') loadAdminAtt(adminRange.days)
  }, [isManager, view, adminRangeKey, loadAdminAtt, adminRange.days])

  // ── Derived: employee clock state ─────────────────────────────────
  const hasTimeIn = !!(today && (today.timeInFormatted || today.timeIn))
  const hasTimeOut = !!(today && (today.timeOutFormatted || today.timeOut))
  const isTimeOff = !!(today && isTimeOffStatus(today.status))

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
    lastCoords.current = await getCurrentLocation()
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
      setNotice({ type: 'ok', text: res.message || 'Clocked in.' })
      await loadMine()
      if (isManager) loadTeam(teamDate)
    }
    return res
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
      if (view === 'attendance') loadAdminAtt(adminRange.days)
    }
  }

  const handleEditSchedule = (row) => {
    setEditSchedEmpId(row.employeeId)
    setShowSchedModal(true)
  }
  const closeSchedModal = () => { setShowSchedModal(false); setEditSchedEmpId(null) }

  const handleDeleteSchedule = async (row) => {
    if (!window.confirm(`Delete schedule for ${row.employeeName}?`)) return
    const res = await scheduleApi.remove(row.employeeId)
    setNotice({ type: res.isSuccess ? 'ok' : 'error', text: res.message || (res.isSuccess ? 'Schedule deleted.' : 'Delete failed.') })
    if (res.isSuccess) loadTeam(teamDate)
  }

  // ── Primary action button — hidden entirely for Admin/HR ──────────
  const ActionButton = ({ inHeader }) => {
    if (isManager) return null
    if (isTimeOff) {
      return <button className="btnGhost" disabled>{Icons.umbrella} Time Off</button>
    }
    if (!hasTimeIn) {
      const busy = acting || locating
      return (
        <div className="actionBtns">
          <button className="btnPrimary" onClick={startTimeIn} disabled={busy}>
            {busy ? <span className="spinner" /> : Icons.clock}
            {locating ? 'Locating…' : acting ? 'Sending code…' : 'Time In'}
          </button>
          <button className="btnGhost" onClick={clockTimeOff} disabled={busy}>
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
    monitoring: { title: isManager ? 'Dashboard' : 'Attendance monitoring', sub: isManager ? 'Today's attendance overview.' : 'Clock in against your schedule — verified by an emailed one-time code.' },
    attendance: { title: isManager ? 'All attendance' : 'My attendance', sub: isManager ? 'Every employee's attendance records.' : 'Your attendance history, location and on-time rate.' },
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

                  <MonitoringTable
                    loading={teamLoading}
                    emptyText="No attendance recorded for this date."
                    filterKeys={['employeeName', 'department', 'status']}
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

                  <BranchEditor onChanged={() => loadTeam(teamDate)} />
                </>
              ) : (
                /* ─── Employee monitoring ─── */
                <>
                  <div className="split">
                    <div className="panel">
                      <h3 className="panel__title">Today · {localDateStr()}</h3>
                      <div className="statusBig">
                        <span className="statusBig__dot" style={{ background: hasTimeIn ? statusColor(today?.status) : 'var(--text-disabled)' }} />
                        <div>
                          <div className="statusBig__label">
                            {isTimeOff ? 'Time Off' : !hasTimeIn ? 'Not clocked in' : hasTimeOut ? 'Completed' : (today?.status || 'Clocked in')}
                          </div>
                          <div className="statusBig__sub">
                            {hasTimeIn
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
                  </div>
                  <MonitoringTable
                    loading={adminAttLoading}
                    emptyText="No attendance records in this range."
                    filterKeys={['employeeName', 'department', 'status', 'date']}
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
