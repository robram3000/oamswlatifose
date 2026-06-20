import { useCallback, useEffect, useRef, useState } from 'react'
import { attendanceApi, workEventApi } from '../../lib/api'
import { Icons } from '../../lib/ui'

// Module-level cache: year → Map<"YYYY-MM-DD", name>
// Avoids re-fetching when the user navigates back to the same year.
const holidayCache = {}

async function fetchHolidays(year) {
  if (holidayCache[year]) return holidayCache[year]
  try {
    const res = await fetch(
      `https://date.nager.at/api/v3/PublicHolidays/${year}/PH`,
      { signal: AbortSignal.timeout(6000) },
    )
    if (!res.ok) throw new Error(`HTTP ${res.status}`)
    const data = await res.json()
    const map = {}
    if (Array.isArray(data)) {
      data.forEach((h) => {
        if (h.date) map[h.date] = h.localName || h.name
      })
    }
    holidayCache[year] = map
    return map
  } catch {
    // Network failure or timeout — return empty so the calendar still works
    holidayCache[year] = {}
    return {}
  }
}

// workDays string: "Mon,Tue,Wed,Thu,Fri"
const WORK_DAY_MAP = { Sun: 0, Mon: 1, Tue: 2, Wed: 3, Thu: 4, Fri: 5, Sat: 6 }

function parseWorkDays(workDays) {
  if (!workDays) return new Set([1, 2, 3, 4, 5])
  return new Set(
    workDays.split(',').map((d) => WORK_DAY_MAP[d.trim()]).filter((n) => n !== undefined),
  )
}

const pad = (n) => String(n).padStart(2, '0')
const isoDate = (y, m, d) => `${y}-${pad(m)}-${pad(d)}`
const todayStr = () => {
  const n = new Date()
  return isoDate(n.getFullYear(), n.getMonth() + 1, n.getDate())
}

const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
]

function statusStyle(status) {
  if (/present|on time/i.test(status || '')) return { label: 'Present',         color: 'var(--gcp-green)', bg: 'rgba(52,168,83,.12)' }
  if (/late/i.test(status || ''))             return { label: 'Late',            color: '#e37400',          bg: 'rgba(251,188,4,.18)' }
  if (/absent/i.test(status || ''))           return { label: 'Absent',          color: 'var(--gcp-red)',   bg: 'rgba(234,67,53,.12)' }
  if (/time.?off|leave/i.test(status || '')) return { label: 'Leave / Time Off', color: 'var(--gcp-blue)', bg: 'rgba(66,133,244,.12)' }
  if (/holiday/i.test(status || ''))          return { label: 'Holiday',         color: '#9334e6',          bg: 'rgba(147,52,230,.12)' }
  return { label: status || '—', color: 'var(--text-muted)', bg: 'transparent' }
}

const WE_STYLE = {
  Holiday: { color: '#9334e6', bg: 'rgba(147,52,230,.12)' },
  DayOff:  { color: 'var(--text-muted)', bg: 'rgba(120,120,120,.10)' },
  Closed:  { color: 'var(--gcp-red)', bg: 'rgba(234,67,53,.10)' },
}

function DayDetail({ day, record, holiday, workEvent, isOff, onClose }) {
  const ss = record ? statusStyle(record.status) : null
  const label = new Date(`${day}T12:00:00`).toLocaleDateString('en-PH', {
    weekday: 'long', year: 'numeric', month: 'long', day: 'numeric',
  })

  return (
    <div className="modalOverlay" onClick={onClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 380 }}>
        <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', marginBottom: 12 }}>
          <div>
            <div style={{ fontWeight: 600, fontSize: 15 }}>{label}</div>
            {holiday && <div style={{ fontSize: 12, color: '#9334e6', marginTop: 2 }}>{holiday}</div>}
            {workEvent && <div style={{ fontSize: 12, color: WE_STYLE[workEvent.type]?.color || 'var(--text-muted)', marginTop: 2 }}>{workEvent.name} ({workEvent.type})</div>}
          </div>
          <button className="iconBtn" onClick={onClose}>{Icons.close}</button>
        </div>

        {workEvent?.type === 'Closed' && (
          <div style={{ padding: '10px 14px', borderRadius: 8, background: 'rgba(234,67,53,.08)', color: 'var(--gcp-red)', fontSize: 13, marginBottom: 8 }}>
            Attendance closed — {workEvent.name}
          </div>
        )}

        {isOff && !record && (
          <div style={{ padding: '10px 14px', borderRadius: 8, background: 'rgba(100,100,100,.08)', color: 'var(--text-muted)', fontSize: 13 }}>
            Weekly off — no scheduled work
          </div>
        )}

        {holiday && !record && !isOff && (
          <div style={{ padding: '10px 14px', borderRadius: 8, background: 'rgba(147,52,230,.10)', color: '#9334e6', fontSize: 13 }}>
            Public holiday — {holiday}
          </div>
        )}

        {!record && !isOff && !holiday && (
          <div style={{ padding: '10px 14px', borderRadius: 8, background: 'rgba(234,67,53,.08)', color: 'var(--gcp-red)', fontSize: 13 }}>
            No attendance record
          </div>
        )}

        {record && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
            <div style={{ display: 'inline-flex', padding: '4px 10px', borderRadius: 20, background: ss.bg, color: ss.color, fontWeight: 600, fontSize: 13, alignSelf: 'flex-start' }}>
              {ss.label}
            </div>
            <div className="kv"><span className="kv__k">Time In</span>  <span className="kv__v">{record.timeInFormatted  || record.timeIn  || '—'}</span></div>
            <div className="kv"><span className="kv__k">Time Out</span> <span className="kv__v">{record.timeOutFormatted || record.timeOut || '—'}</span></div>
            <div className="kv"><span className="kv__k">Hours</span>    <span className="kv__v">{record.hoursWorkedFormatted || record.hoursWorked || '—'}</span></div>
            <div className="kv"><span className="kv__k">Location</span> <span className="kv__v">{record.workLocation || '—'}{record.branchName ? ` · ${record.branchName}` : ''}</span></div>
            {record.remarks && <div className="kv"><span className="kv__k">Remarks</span><span className="kv__v">{record.remarks}</span></div>}
          </div>
        )}
      </div>
    </div>
  )
}

export default function AttendanceCalendar({ schedule }) {
  const now = new Date()
  const [year, setYear]       = useState(now.getFullYear())
  const [month, setMonth]     = useState(now.getMonth() + 1) // 1-based
  const [records, setRecords] = useState([])
  const [loadingAtt, setLoadingAtt] = useState(true)
  const [holidays, setHolidays]     = useState({}) // "YYYY-MM-DD" → name
  const [workEvents, setWorkEvents] = useState({}) // "YYYY-MM-DD" → { type, name }
  const [detail, setDetail]         = useState(null)
  const today = todayStr()

  const workDays = parseWorkDays(schedule?.workDays)

  // Fetch attendance records for the visible month
  const loadAtt = useCallback(async (y, m) => {
    setLoadingAtt(true)
    const res = await attendanceApi.myCalendar(y, m)
    setRecords(
      res.isSuccess
        ? (Array.isArray(res.data) ? res.data : (res.data?.items ?? []))
        : [],
    )
    setLoadingAtt(false)
  }, [])

  // Fetch holidays from Nager.Date API for the visible year
  const loadHolidays = useCallback(async (y) => {
    const map = await fetchHolidays(y)
    setHolidays(map)
  }, [])

  // Fetch custom work events (HR-created holidays/off days/closed days) for the visible month
  const loadWorkEvents = useCallback(async (y, m) => {
    const res = await workEventApi.byMonth(y, m)
    const map = {}
    if (res.isSuccess && Array.isArray(res.data)) {
      res.data.forEach((e) => { if (e.date) map[e.date] = { type: e.eventType, name: e.name } })
    }
    setWorkEvents(map)
  }, [])

  useEffect(() => { loadAtt(year, month) }, [loadAtt, year, month])
  useEffect(() => { loadHolidays(year) }, [loadHolidays, year])
  useEffect(() => { loadWorkEvents(year, month) }, [loadWorkEvents, year, month])

  const prevMonth = () => {
    if (month === 1) { setYear((y) => y - 1); setMonth(12) }
    else setMonth((m) => m - 1)
  }
  const nextMonth = () => {
    if (month === 12) { setYear((y) => y + 1); setMonth(1) }
    else setMonth((m) => m + 1)
  }
  const goToday = () => { setYear(now.getFullYear()); setMonth(now.getMonth() + 1) }

  // Build date → record map
  const recordMap = {}
  records.forEach((r) => {
    let d = r.attendanceDate
      ? (typeof r.attendanceDate === 'string'
          ? r.attendanceDate.slice(0, 10)
          : isoDate(new Date(r.attendanceDate).getFullYear(), new Date(r.attendanceDate).getMonth() + 1, new Date(r.attendanceDate).getDate()))
      : (r.dateFormatted?.slice(0, 10) || r.date?.slice(0, 10))
    if (d) recordMap[d] = r
  })

  // Calendar grid
  const firstDow    = new Date(year, month - 1, 1).getDay()
  const daysInMonth = new Date(year, month, 0).getDate()
  const cells = [
    ...Array(firstDow).fill(null),
    ...Array.from({ length: daysInMonth }, (_, i) => i + 1),
  ]

  const openDay = (d) => {
    const ds  = isoDate(year, month, d)
    const dow = new Date(year, month - 1, d).getDay()
    const we  = workEvents[ds] || null
    setDetail({
      day:       ds,
      record:    recordMap[ds] || null,
      holiday:   holidays[ds] || null,
      workEvent: we,
      isOff:     !workDays.has(dow),
    })
  }

  const isCurrentMonth = year === now.getFullYear() && month === now.getMonth() + 1

  return (
    <div className="calendarWrap">
      {/* Navigation */}
      <div className="calendarNav">
        <button className="iconBtn" onClick={prevMonth} title="Previous month">{Icons.chevLeft}</button>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <span className="calendarNavTitle">{MONTH_NAMES[month - 1]} {year}</span>
          {!isCurrentMonth && (
            <button className="btnGhost" style={{ padding: '2px 10px', fontSize: 12 }} onClick={goToday}>Today</button>
          )}
        </div>
        <button className="iconBtn" onClick={nextMonth} title="Next month">{Icons.chevRight}</button>
      </div>

      {loadingAtt ? (
        <div style={{ textAlign: 'center', padding: 48, color: 'var(--text-muted)' }}>
          <span className="spinner spinner--blue" style={{ display: 'inline-block', marginBottom: 10 }} />
          <div style={{ fontSize: 13 }}>Loading attendance…</div>
        </div>
      ) : (
        <>
          <div className="calendarGrid">
            {['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'].map((d) => (
              <div key={d} className="calendarDow">{d}</div>
            ))}

            {cells.map((d, i) => {
              if (d === null) return <div key={`pad-${i}`} className="calendarCell calendarCell--empty" />

              const ds        = isoDate(year, month, d)
              const record    = recordMap[ds]
              const dow       = new Date(year, month - 1, d).getDay()
              const isOff     = !workDays.has(dow)
              const holiday   = holidays[ds] || null
              const workEvent = workEvents[ds] || null
              const isToday   = ds === today
              const isFuture  = ds > today
              const ss        = record ? statusStyle(record.status) : null
              const weStyle   = workEvent ? WE_STYLE[workEvent.type] : null

              return (
                <div
                  key={ds}
                  className={[
                    'calendarCell',
                    isToday  ? 'calendarCell--today'  : '',
                    isFuture ? 'calendarCell--future' : '',
                  ].join(' ').trim()}
                  onClick={() => openDay(d)}
                  title={holiday || undefined}
                >
                  <span className="calendarDay">{d}</span>

                  {holiday && (
                    <span className="calendarChip" style={{ background: 'rgba(147,52,230,.15)', color: '#9334e6' }}>
                      {holiday}
                    </span>
                  )}

                  {workEvent && (
                    <span className="calendarChip" style={{ background: weStyle?.bg, color: weStyle?.color }}>
                      {workEvent.name}
                    </span>
                  )}

                  {record && (
                    <span className="calendarChip" style={{ background: ss.bg, color: ss.color }}>
                      {ss.label}
                    </span>
                  )}

                  {!record && isOff && !isFuture && !holiday && (
                    <span className="calendarChip calendarChip--off">Weekly Off</span>
                  )}

                  {!record && !isOff && !holiday && !isFuture && !isToday && (
                    <span className="calendarChip" style={{ background: 'rgba(234,67,53,.10)', color: 'var(--gcp-red)' }}>
                      Absent
                    </span>
                  )}
                </div>
              )
            })}
          </div>

          <div className="calendarLegend">
            {[
              { label: 'Present',         color: 'var(--gcp-green)', bg: 'rgba(52,168,83,.12)' },
              { label: 'Late',            color: '#e37400',          bg: 'rgba(251,188,4,.18)' },
              { label: 'Absent',          color: 'var(--gcp-red)',   bg: 'rgba(234,67,53,.10)' },
              { label: 'Leave / Time Off',color: 'var(--gcp-blue)',  bg: 'rgba(66,133,244,.12)' },
              { label: 'Holiday',         color: '#9334e6',          bg: 'rgba(147,52,230,.15)' },
              { label: 'Weekly Off',      color: 'var(--text-muted)',bg: 'rgba(120,120,120,.10)' },
            ].map((l) => (
              <span key={l.label} className="calendarLegendItem">
                <span style={{ width: 10, height: 10, borderRadius: 3, background: l.bg, border: `1.5px solid ${l.color}`, flexShrink: 0, display: 'inline-block' }} />
                <span style={{ color: l.color, fontSize: 11, fontWeight: 500 }}>{l.label}</span>
              </span>
            ))}
          </div>
        </>
      )}

      {detail && (
        <DayDetail
          day={detail.day}
          record={detail.record}
          holiday={detail.holiday}
          workEvent={detail.workEvent}
          isOff={detail.isOff}
          onClose={() => setDetail(null)}
        />
      )}
    </div>
  )
}
