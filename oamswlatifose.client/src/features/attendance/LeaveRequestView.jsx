import { useCallback, useEffect, useState } from 'react'
import { leaveApi, auth } from '../../lib/api'
import { Icons } from '../../lib/ui'

const LEAVE_TYPES = ['Annual', 'Sick', 'Emergency', 'Other']

const STATUS_STYLE = {
  Pending:  { color: '#e37400',          bg: 'rgba(251,188,4,.18)' },
  Approved: { color: 'var(--gcp-green)', bg: 'rgba(52,168,83,.12)' },
  Rejected: { color: 'var(--gcp-red)',   bg: 'rgba(234,67,53,.12)' },
}

function StatusBadge({ status }) {
  const s = STATUS_STYLE[status] || { color: 'var(--text-muted)', bg: 'transparent' }
  return (
    <span style={{ padding: '2px 10px', borderRadius: 20, background: s.bg, color: s.color, fontSize: 12, fontWeight: 600 }}>
      {status}
    </span>
  )
}

function LeaveRow({ row, onCancel, onApprove, showEmployee }) {
  const isManager = auth.isManager
  const [approveNote, setApproveNote] = useState('')
  const [approving, setApproving] = useState(false)
  const [showApprove, setShowApprove] = useState(false)
  const [noticeText, setNoticeText] = useState(null)

  const doApprove = async (isApproved) => {
    setApproving(true)
    const res = await leaveApi.approve(row.id, { isApproved, note: approveNote })
    setApproving(false)
    if (res.isSuccess) { setShowApprove(false); onApprove() }
    else setNoticeText(res.message)
  }

  return (
    <div style={{ padding: '14px 0', borderBottom: '1px solid var(--border-color)' }}>
      <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: 12, flexWrap: 'wrap' }}>
        <div style={{ flex: 1, minWidth: 180 }}>
          {showEmployee && <div style={{ fontWeight: 600, fontSize: 14, marginBottom: 2 }}>{row.employeeName}</div>}
          <div style={{ fontSize: 13, color: 'var(--text-secondary)' }}>
            <span style={{ fontWeight: 600 }}>{row.leaveType}</span> · {row.startDate === row.endDate ? row.startDate : `${row.startDate} → ${row.endDate}`}
          </div>
          {row.reason && <div style={{ fontSize: 12, color: 'var(--text-muted)', marginTop: 3 }}>{row.reason}</div>}
          {row.approvalNote && row.status !== 'Pending' && (
            <div style={{ fontSize: 12, color: 'var(--text-secondary)', marginTop: 3, fontStyle: 'italic' }}>
              Note: {row.approvalNote}
            </div>
          )}
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <StatusBadge status={row.status} />
          {row.status === 'Pending' && isManager && (
            <button className="btnSm" onClick={() => setShowApprove(v => !v)}>Review</button>
          )}
          {row.status === 'Pending' && !isManager && (
            <button className="btnSm btnSm--danger" onClick={() => onCancel(row.id)}>Cancel</button>
          )}
        </div>
      </div>

      {showApprove && (
        <div style={{ marginTop: 10, padding: '12px 14px', background: 'var(--surface-2)', borderRadius: 8 }}>
          {noticeText && <p className="alert alert--error" style={{ margin: '0 0 8px' }}>{noticeText}</p>}
          <div className="field" style={{ marginBottom: 8 }}>
            <label style={{ fontSize: 12 }}>Note (optional)</label>
            <input className="input" value={approveNote} onChange={e => setApproveNote(e.target.value)}
              placeholder="Reason for approval or rejection" />
          </div>
          <div style={{ display: 'flex', gap: 8 }}>
            <button className="btnPrimary" style={{ background: 'var(--gcp-green)', borderColor: 'var(--gcp-green)' }}
              onClick={() => doApprove(true)} disabled={approving}>
              {Icons.check} Approve
            </button>
            <button className="btnSm btnSm--danger" onClick={() => doApprove(false)} disabled={approving}>
              Reject
            </button>
            <button className="btnGhost" onClick={() => setShowApprove(false)}>Cancel</button>
          </div>
        </div>
      )}
    </div>
  )
}

function SubmitForm({ onSubmitted }) {
  const today = new Date().toISOString().slice(0, 10)
  const [form, setForm] = useState({ startDate: today, endDate: today, leaveType: 'Annual', reason: '' })
  const [saving, setSaving] = useState(false)
  const [notice, setNotice] = useState(null)

  const set = (k, v) => setForm(f => ({ ...f, [k]: v }))

  const submit = async () => {
    setNotice(null)
    if (!form.startDate || !form.endDate) { setNotice({ type: 'error', text: 'Start and end dates are required.' }); return }
    if (form.endDate < form.startDate) { setNotice({ type: 'error', text: 'End date must be on or after start date.' }); return }

    setSaving(true)
    const res = await leaveApi.submit({
      startDate: form.startDate,
      endDate: form.endDate,
      leaveType: form.leaveType,
      reason: form.reason,
    })
    setSaving(false)

    if (res.isSuccess) {
      setForm({ startDate: today, endDate: today, leaveType: 'Annual', reason: '' })
      setNotice({ type: 'ok', text: 'Leave request submitted.' })
      onSubmitted()
    } else {
      setNotice({ type: 'error', text: res.message || 'Could not submit leave request.' })
    }
  }

  return (
    <div className="panel">
      <h3 className="panel__title">Request leave</h3>
      {notice && <p className={`alert alert--${notice.type === 'ok' ? 'ok' : 'error'}`}>{notice.text}</p>}
      <div className="fieldRow" style={{ flexWrap: 'wrap' }}>
        <div className="field">
          <label>From *</label>
          <input type="date" className="input" value={form.startDate} onChange={e => set('startDate', e.target.value)} />
        </div>
        <div className="field">
          <label>To *</label>
          <input type="date" className="input" value={form.endDate} onChange={e => set('endDate', e.target.value)} />
        </div>
        <div className="field">
          <label>Type *</label>
          <select className="select" value={form.leaveType} onChange={e => set('leaveType', e.target.value)}>
            {LEAVE_TYPES.map(t => <option key={t} value={t}>{t}</option>)}
          </select>
        </div>
      </div>
      <div className="field">
        <label>Reason</label>
        <input className="input" value={form.reason} onChange={e => set('reason', e.target.value)}
          placeholder="Optional — brief explanation" />
      </div>
      <div className="modal__actions" style={{ justifyContent: 'flex-start' }}>
        <button className="btnPrimary" onClick={submit} disabled={saving}>
          {saving ? 'Submitting…' : 'Submit request'}
        </button>
      </div>
    </div>
  )
}

export default function LeaveRequestView() {
  const isManager = auth.isManager
  const [myLeaves, setMyLeaves] = useState([])
  const [allLeaves, setAllLeaves] = useState([])
  const [loading, setLoading] = useState(true)
  const [filterStatus, setFilterStatus] = useState('')
  const [notice, setNotice] = useState(null)

  const load = useCallback(async () => {
    setLoading(true)
    const [mine, all] = await Promise.all([
      leaveApi.mine(),
      isManager ? leaveApi.all(filterStatus || undefined) : Promise.resolve({ isSuccess: false }),
    ])
    setMyLeaves(mine.isSuccess ? (mine.data ?? []) : [])
    setAllLeaves(all.isSuccess ? (all.data ?? []) : [])
    setLoading(false)
  }, [isManager, filterStatus])

  useEffect(() => { load() }, [load])

  const cancel = async (id) => {
    const res = await leaveApi.cancel(id)
    if (res.isSuccess) { setNotice({ type: 'ok', text: 'Request cancelled.' }); load() }
    else setNotice({ type: 'error', text: res.message })
  }

  return (
    <div>
      {notice && <p className={`alert alert--${notice.type === 'ok' ? 'ok' : 'error'}`}>{notice.text}</p>}

      {/* Employee submit form */}
      {!isManager && <SubmitForm onSubmitted={load} />}

      {/* My leave history */}
      <div className="panel" style={{ marginTop: 16 }}>
        <h3 className="panel__title">My leave requests</h3>
        {loading ? (
          <p className="muted">Loading…</p>
        ) : myLeaves.length === 0 ? (
          <p className="muted" style={{ fontSize: 13 }}>No leave requests yet.</p>
        ) : (
          myLeaves.map(r => (
            <LeaveRow key={r.id} row={r} onCancel={cancel} onApprove={load} showEmployee={false} />
          ))
        )}
      </div>

      {/* HR/Admin: all employees' leave requests */}
      {isManager && (
        <div className="panel" style={{ marginTop: 16 }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 12 }}>
            <h3 className="panel__title" style={{ margin: 0 }}>All leave requests</h3>
            <div style={{ display: 'flex', gap: 8 }}>
              {['', 'Pending', 'Approved', 'Rejected'].map(s => (
                <button key={s} className={`chip ${filterStatus === s ? 'chip--active' : ''}`}
                  onClick={() => setFilterStatus(s)}>
                  {s || 'All'}{filterStatus === s && ' ✓'}
                </button>
              ))}
            </div>
          </div>
          {loading ? (
            <p className="muted">Loading…</p>
          ) : allLeaves.length === 0 ? (
            <p className="muted" style={{ fontSize: 13 }}>No leave requests.</p>
          ) : (
            allLeaves.map(r => (
              <LeaveRow key={r.id} row={r} onCancel={cancel} onApprove={load} showEmployee />
            ))
          )}
        </div>
      )}
    </div>
  )
}
