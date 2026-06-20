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
const isPresent = (s) => /present|on time/i.test(s || '')
const isLate = (s) => /late/i.test(s || '')
const isAbsent = (s) => /absent/i.test(s || '')

export default function AttendanceConsole({ user, onSignOut }) {
  const isManager = auth.isManager
  const [view, setView] = useState('monitoring') // monitoring | attendance | schedule
  const [sidebarOpen, setSidebarOpen] = useState(false)

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
  // GPS captured at Time-In, reused on resend so Office/Outside stays consistent.
  const lastCoords = useRef(null)

  // Manager-only (Admin/HR)
  const [teamDate, setTeamDate] = useState(localDateStr())
  const [teamRows, setTeamRows] = useState([])
  const [teamLoading, setTeamLoading] = useState(false)
  const [schedulesByEmp, setSchedulesByEmp] = useState({})
  const [editSchedEmpId, setEditSchedEmpId] = useState(null)
  const [showSchedModal, setShowSchedModal] = useState(false)
  const [viewSched, setViewSched] = useState(null)

  const range = RANGES.find((r) => r.key === rangeKey) || RANGES[2]

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

  useEffect(() => { loadMine() }, [loadMine])
  useEffect(() => { if (isManager) loadTeam(teamDate) }, [isManager, teamDate, loadTeam])

  // ── Derived: clock state ──────────────────────────────────────────
  const hasTimeIn = !!(today && (today.timeInFormatted || today.timeIn))
  const hasTimeOut = !!(today && (today.timeOutFormatted || today.timeOut))
  const isTimeOff = !!(today && /time.?off/i.test(today.status || ''))

  // ── Derived: range-filtered history + metrics ─────────────────────
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

  // ── Actions ───────────────────────────────────────────────────────
  const requestOtp = useCallback(async () => {
    const res = await attendanceApi.requestOtp(lastCoords.current)
    if (res.isSuccess) setOtpInfo(res.data)
    return res
  }, [])

  const startTimeIn = async () => {
    setNotice(null)
    setLocating(true)
    lastCoords.current = await getCurrentLocation() // null if denied → server records "Outside/Unknown"
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

  const refresh = () => { loadMine(); if (isManager) loadTeam(teamDate) }

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

  // ── Primary action button (varies by clock state) ─────────────────
  const ActionButton = ({ inHeader }) => {
    // Already marked as time off today (employees only)
    if (isTimeOff && !isManager) {
      return <button className="btnGhost" disabled>{Icons.umbrella} Time Off</button>
    }
    // Not clocked in yet
    if (!hasTimeIn) {
      const busy = acting || locating
      return (
        <div className="actionBtns">
          <button className="btnPrimary" onClick={startTimeIn} disabled={busy}>
            {busy ? <span className="spinner" /> : Icons.clock}
            {locating ? 'Locating…' : acting ? 'Sending code…' : 'Time In'}
          </button>
          {/* Time Off is an employee-only action; admins manipulate records directly */}
          {!isManager && (
            <button className="btnGhost" onClick={clockTimeOff} disabled={busy}>
              {Icons.umbrella} Time Off
            </button>
          )}
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

  const mySchedulePanel = (
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
        <p className="alert alert--info">
          No schedule set yet. {isManager ? 'Set one under Schedule below.' : 'Ask Admin/HR to set your schedule — clock-ins default to a 09:00 start until then.'}
        </p>
      )}
    </div>
  )

  const VIEWS = {
    monitoring: { title: 'Attendance monitoring', sub: 'Clock in against your schedule — verified by an emailed one-time code.' },
    attendance: { title: 'My attendance', sub: 'Your attendance history, location and on-time rate.' },
    schedule: { title: 'Schedule', sub: isManager ? 'Set work schedules per employee and review everyone’s.' : 'Your assigned work schedule.' },
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
      {/* Sidebar overlay (mobile only) */}
      {sidebarOpen && <div className="sidebarOverlay" onClick={() => setSidebarOpen(false)} />}

      {/* Sidebar */}
      <aside className={`sidebar${sidebarOpen ? ' sidebar--open' : ''}`}>
        <div className="sidebar__brand">
          <img src="/logo.svg" alt="AGLIPAY" className="brandLogo" />
          <div className="brandText">
            <span className="brandTitle">AGLIPAY</span>
            <span className="brandSub">Attendance monitoring</span>
          </div>
        </div>
        {navItem('monitoring', Icons.monitor, 'Monitoring')}
        {navItem('attendance', Icons.clock, 'My attendance')}
        {navItem('schedule', Icons.calendar, 'Schedule')}
        {isManager && navItem('users', Icons.users, 'Users')}
      </aside>

      {/* Main */}
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
          {/* Header */}
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

          {/* ===================== MONITORING ===================== */}
          {view === 'monitoring' && (
            <>
              <div className="split">
                <div className="panel">
                  <h3 className="panel__title">Today · {localDateStr()}</h3>
                  <div className="statusBig">
                    <span className="statusBig__dot"
                          style={{ background: hasTimeIn ? statusColor(today?.status) : 'var(--text-disabled)' }} />
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

              {isManager && (
                <>
                  <div className="topRow" style={{ marginTop: 8 }}>
                    <div>
                      <h1 className="pageTitle" style={{ fontSize: 18 }}>Team monitoring</h1>
                      <p className="pageSub">Everyone’s attendance — Office vs Outside — for a given day.</p>
                    </div>
                    <div className="field">
                      <label htmlFor="td">Date</label>
                      <input id="td" type="date" className="input" value={teamDate} onChange={(e) => setTeamDate(e.target.value)} />
                    </div>
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
                        <>
                          <span className="statusDot" style={{ background: statusColor(r.status) }} />
                          {r.employeeName}
                        </>
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
              )}
            </>
          )}

          {/* ===================== MY ATTENDANCE ===================== */}
          {view === 'attendance' && (
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

          {/* ===================== SCHEDULE ===================== */}
          {view === 'schedule' && (
            <>
              {mySchedulePanel}

              {isManager ? (
                <>
                  <div className="topRow" style={{ marginTop: 8 }}>
                    <div>
                      <h1 className="pageTitle" style={{ fontSize: 18 }}>All schedules</h1>
                      <p className="pageSub">Every employee’s active work schedule.</p>
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

          {/* ===================== USERS (Admin/HR) ===================== */}
          {view === 'users' && isManager && <UserManager />}
        </div>
      </div>

      {otpInfo && (
        <OtpModal
          info={otpInfo}
          onVerify={verify}
          onResend={requestOtp}
          onClose={() => setOtpInfo(null)}
        />
      )}

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
