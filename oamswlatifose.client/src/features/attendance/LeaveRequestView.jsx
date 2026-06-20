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

// ── Request leave modal ───────────────────────────────────────────────
function RequestLeaveModal({ onClose, onSubmitted }) {
  const today = new Date().toISOString().slice(0, 10)
  const [form, setForm] = useState({ startDate: today, endDate: today, leaveType: 'Annual', reason: '' })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState(null)

  const set = (k, v) => setForm((f) => ({ ...f, [k]: v }))

  const days =
    form.startDate && form.endDate && form.endDate >= form.startDate
      ? Math.round((new Date(form.endDate) - new Date(form.startDate)) / 86400000) + 1
      : 0

  const submit = async () => {
    setError(null)
    if (!form.startDate || !form.endDate) { setError('Start and end dates are required.'); return }
    if (form.endDate < form.startDate) { setError('End date must be on or after start date.'); return }
    setSaving(true)
    const res = await leaveApi.submit({
      startDate: form.startDate,
      endDate: form.endDate,
      leaveType: form.leaveType,
      reason: form.reason,
    })
    setSaving(false)
    if (res.isSuccess) { onSubmitted(); onClose() }
    else setError(res.message || 'Could not submit leave request.')
  }

  return (
    <div className="modalOverlay" onClick={onClose}>
      <div className="modal modal--wide" onClick={(e) => e.stopPropagation()}>
        <div className="modal__header">
          <h3 className="modal__title" style={{ margin: 0 }}>Request leave</h3>
          <button className="iconBtn" onClick={onClose}>{Icons.close}</button>
        </div>
        <div style={{ padding: '0 24px 24px' }}>
          {error && <p className="alert alert--error" style={{ margin: '12px 0' }}>{error}</p>}

          <div className="fieldRow" style={{ marginTop: 16, flexWrap: 'wrap' }}>
            <div className="field">
              <label>From *</label>
              <input type="date" className="input" value={form.startDate}
                onChange={(e) => set('startDate', e.target.value)} />
            </div>
            <div className="field">
              <label>To *</label>
              <input type="date" className="input" value={form.endDate}
                onChange={(e) => set('endDate', e.target.value)} />
            </div>
            <div className="field">
              <label>Type *</label>
              <select className="select" value={form.leaveType} onChange={(e) => set('leaveType', e.target.value)}>
                {LEAVE_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
              </select>
            </div>
          </div>

          {days > 0 && (
            <p className="muted" style={{ fontSize: 12, margin: '-4px 0 12px' }}>
              {days} day{days !== 1 ? 's' : ''} of {form.leaveType} leave
            </p>
          )}

          <div className="field">
            <label>Reason <span className="muted">(optional)</span></label>
            <input className="input" value={form.reason}
              onChange={(e) => set('reason', e.target.value)}
              placeholder="Brief explanation"
              onKeyDown={(e) => e.key === 'Enter' && submit()} />
          </div>

          <div className="modal__actions">
            <button className="btnGhost" onClick={onClose}>Cancel</button>
            <button className="btnPrimary" onClick={submit} disabled={saving}>
              {saving ? 'Submitting…' : 'Submit request'}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}

// ── Review (approve / reject) modal ──────────────────────────────────
function ReviewModal({ row, onClose, onDone }) {
  const [note, setNote] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState(null)

  const act = async (isApproved) => {
    setSaving(true)
    setError(null)
    const res = await leaveApi.approve(row.id, { isApproved, note })
    setSaving(false)
    if (res.isSuccess) { onDone(); onClose() }
    else setError(res.message || 'Action failed.')
  }

  const daysCount =
    row.startDate && row.endDate
      ? Math.round((new Date(row.endDate) - new Date(row.startDate)) / 86400000) + 1
      : 1

  return (
    <div className="modalOverlay" onClick={onClose}>
      <div className="modal modal--wide" onClick={(e) => e.stopPropagation()}>
        <div className="modal__header">
          <h3 className="modal__title" style={{ margin: 0 }}>Review leave request</h3>
          <button className="iconBtn" onClick={onClose}>{Icons.close}</button>
        </div>
        <div style={{ padding: '0 24px 24px' }}>
          {error && <p className="alert alert--error" style={{ margin: '12px 0' }}>{error}</p>}

          <div style={{ padding: '14px 0', borderBottom: '1px solid var(--border-color)', marginBottom: 16 }}>
            <div style={{ fontWeight: 600, fontSize: 15, marginBottom: 4 }}>{row.employeeName}</div>
            <div style={{ fontSize: 13, color: 'var(--text-secondary)' }}>
              <span style={{ fontWeight: 600 }}>{row.leaveType}</span>
              {' · '}
              {row.startDate === row.endDate ? row.startDate : `${row.startDate} → ${row.endDate}`}
              {' · '}
              {daysCount} day{daysCount !== 1 ? 's' : ''}
            </div>
            {row.reason && (
              <div style={{ fontSize: 12, color: 'var(--text-muted)', marginTop: 6 }}>"{row.reason}"</div>
            )}
          </div>

          <div className="field">
            <label>Note <span className="muted">(optional — shown to employee)</span></label>
            <input className="input" value={note} onChange={(e) => setNote(e.target.value)}
              placeholder="Reason for approval or rejection" />
          </div>

          <div className="modal__actions">
            <button className="btnGhost" onClick={onClose} disabled={saving}>Cancel</button>
            <button className="btnSm btnSm--danger" onClick={() => act(false)} disabled={saving}>
              Reject
            </button>
            <button className="btnPrimary" onClick={() => act(true)} disabled={saving}
              style={{ background: 'var(--gcp-green)', borderColor: 'var(--gcp-green)' }}>
              {saving ? 'Saving…' : `${Icons.check} Approve`}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}

// ── Leave row ─────────────────────────────────────────────────────────
function LeaveRow({ row, onCancel, onApprove, showEmployee }) {
  const isManager = auth.isManager
  const [showReview, setShowReview] = useState(false)

  return (
    <>
      <div style={{ padding: '14px 0', borderBottom: '1px solid var(--border-color)' }}>
        <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: 12, flexWrap: 'wrap' }}>
          <div style={{ flex: 1, minWidth: 180 }}>
            {showEmployee && <div style={{ fontWeight: 600, fontSize: 14, marginBottom: 2 }}>{row.employeeName}</div>}
            <div style={{ fontSize: 13, color: 'var(--text-secondary)' }}>
              <span style={{ fontWeight: 600 }}>{row.leaveType}</span>
              {' · '}
              {row.startDate === row.endDate ? row.startDate : `${row.startDate} → ${row.endDate}`}
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
              <button className="btnSm" onClick={() => setShowReview(true)}>Review</button>
            )}
            {row.status === 'Pending' && !isManager && (
              <button className="btnSm btnSm--danger" onClick={() => onCancel(row.id)}>Cancel</button>
            )}
          </div>
        </div>
      </div>

      {showReview && (
        <ReviewModal
          row={row}
          onClose={() => setShowReview(false)}
          onDone={onApprove}
        />
      )}
    </>
  )
}

// ── Main view ─────────────────────────────────────────────────────────
export default function LeaveRequestView() {
  const isManager = auth.isManager
  const [myLeaves, setMyLeaves] = useState([])
  const [allLeaves, setAllLeaves] = useState([])
  const [loading, setLoading] = useState(true)
  const [filterStatus, setFilterStatus] = useState('')
  const [notice, setNotice] = useState(null)
  const [showRequest, setShowRequest] = useState(false)

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
      {notice && (
        <p className={`alert alert--${notice.type === 'ok' ? 'ok' : 'error'}`}>{notice.text}</p>
      )}

      {/* My leave history */}
      <div className="panel">
        <div className="topRow" style={{ marginBottom: 14 }}>
          <div>
            <h3 className="panel__title" style={{ margin: 0 }}>My leave requests</h3>
            <p className="pageSub" style={{ marginTop: 2 }}>Your filed leave requests and their status.</p>
          </div>
          {!isManager && (
            <button className="btnPrimary" onClick={() => setShowRequest(true)}>
              {Icons.plus} Request leave
            </button>
          )}
        </div>

        {loading ? (
          <p className="muted">Loading…</p>
        ) : myLeaves.length === 0 ? (
          <p className="muted" style={{ fontSize: 13 }}>
            No leave requests yet.{!isManager && ' Click "Request leave" to file one.'}
          </p>
        ) : (
          myLeaves.map((r) => (
            <LeaveRow key={r.id} row={r} onCancel={cancel} onApprove={load} showEmployee={false} />
          ))
        )}
      </div>

      {/* HR/Admin: all employees' leave requests */}
      {isManager && (
        <div className="panel" style={{ marginTop: 16 }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 12, flexWrap: 'wrap', gap: 8 }}>
            <h3 className="panel__title" style={{ margin: 0 }}>All leave requests</h3>
            <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
              {['', 'Pending', 'Approved', 'Rejected'].map((s) => (
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
            allLeaves.map((r) => (
              <LeaveRow key={r.id} row={r} onCancel={cancel} onApprove={load} showEmployee />
            ))
          )}
        </div>
      )}

      {showRequest && (
        <RequestLeaveModal
          onClose={() => setShowRequest(false)}
          onSubmitted={() => { setNotice({ type: 'ok', text: 'Leave request submitted.' }); load() }}
        />
      )}
    </div>
  )
}
