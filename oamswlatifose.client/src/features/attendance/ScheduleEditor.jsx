import { useEffect, useMemo, useState } from 'react'
import { scheduleApi, attendanceApi } from '../../lib/api'

const DAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun']

function empName(e) {
  return e.fullName || `${e.firstName || ''} ${e.lastName || ''}`.trim() || `Employee #${e.id}`
}

// Admin/Manager panel to set "the schedule time" an employee's attendance is graded against.
// `schedulesByEmp` (employeeId -> WorkScheduleDTO) is used to prefill when an employee is picked.
export default function ScheduleEditor({ schedulesByEmp = {}, onSaved }) {
  const [employees, setEmployees] = useState([])
  const [employeeId, setEmployeeId] = useState('')
  const [start, setStart] = useState('08:00')
  const [end, setEnd] = useState('17:00')
  const [grace, setGrace] = useState(5)
  const [days, setDays] = useState(['Mon', 'Tue', 'Wed', 'Thu', 'Fri'])
  const [saving, setSaving] = useState(false)
  const [notice, setNotice] = useState(null)

  useEffect(() => {
    let alive = true
    attendanceApi.employees().then((res) => {
      if (!alive) return
      const list = res?.data?.items ?? res?.data ?? []
      setEmployees(Array.isArray(list) ? list : [])
    })
    return () => { alive = false }
  }, [])

  // Prefill from the existing schedule when an employee is selected.
  useEffect(() => {
    if (!employeeId) return
    const s = schedulesByEmp[employeeId]
    if (s) {
      setStart(s.startTime || '08:00')
      setEnd(s.endTime || '17:00')
      setGrace(s.graceMinutes ?? 5)
      setDays((s.workDays || '').split(',').map((d) => d.trim()).filter(Boolean))
    }
  }, [employeeId, schedulesByEmp])

  const sortedEmployees = useMemo(
    () => [...employees].sort((a, b) => empName(a).localeCompare(empName(b))),
    [employees],
  )

  const toggleDay = (d) =>
    setDays((cur) => (cur.includes(d) ? cur.filter((x) => x !== d) : [...cur, d]))

  const save = async () => {
    setNotice(null)
    if (!employeeId) { setNotice({ type: 'error', text: 'Pick an employee first.' }); return }
    setSaving(true)
    const res = await scheduleApi.set({
      employeeId: Number(employeeId),
      startTime: start,
      endTime: end,
      graceMinutes: Number(grace),
      workDays: DAYS.filter((d) => days.includes(d)).join(','),
    })
    setSaving(false)
    if (res.isSuccess) {
      setNotice({ type: 'ok', text: 'Schedule saved.' })
      onSaved?.(res.data)
    } else {
      setNotice({ type: 'error', text: res.message || 'Could not save schedule.' })
    }
  }

  return (
    <div className="panel">
      <h3 className="panel__title">Set work schedule</h3>

      {notice && (
        <p className={`alert ${notice.type === 'ok' ? 'alert--ok' : 'alert--error'}`} style={{ marginBottom: 14 }}>
          {notice.text}
        </p>
      )}

      <div className="fieldRow">
        <div className="field" style={{ minWidth: 220 }}>
          <label htmlFor="emp">Employee</label>
          <select id="emp" className="select" value={employeeId} onChange={(e) => setEmployeeId(e.target.value)}>
            <option value="">Select employee…</option>
            {sortedEmployees.map((e) => (
              <option key={e.id} value={e.id}>{empName(e)}{e.department ? ` — ${e.department}` : ''}</option>
            ))}
          </select>
        </div>
        <div className="field">
          <label htmlFor="st">Start time</label>
          <input id="st" type="time" className="input" value={start} onChange={(e) => setStart(e.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="en">End time</label>
          <input id="en" type="time" className="input" value={end} onChange={(e) => setEnd(e.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="gr">Grace (min)</label>
          <input id="gr" type="number" min="0" max="120" className="input" style={{ minWidth: 90 }}
                 value={grace} onChange={(e) => setGrace(e.target.value)} />
        </div>
      </div>

      <div className="field" style={{ marginTop: 14 }}>
        <label>Work days</label>
        <div className="rangeRow">
          {DAYS.map((d) => (
            <button key={d} type="button" className={`chip ${days.includes(d) ? 'chip--active' : ''}`} onClick={() => toggleDay(d)}>
              {d}
            </button>
          ))}
        </div>
      </div>

      <div className="actions">
        <button className="btnPrimary" onClick={save} disabled={saving}>
          {saving ? 'Saving…' : 'Save schedule'}
        </button>
      </div>
    </div>
  )
}
