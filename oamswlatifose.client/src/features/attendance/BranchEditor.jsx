import { useEffect, useRef, useState } from 'react'
import { branchApi, usersApi } from '../../lib/api'
import { getCurrentLocation } from '../../lib/geo'
import { Icons } from '../../lib/ui'
import ConfirmDeleteModal from './ConfirmDeleteModal'
import BranchMap from './BranchMap'

const EMPTY = {
  id: null, name: '', address: '', latitude: '', longitude: '', radiusMeters: 100,
  isActive: true, employeeIds: [], geofenceType: 'circle', polygon: [],
}

function BranchFormModal({ initial, onSave, onClose }) {
  const [form, setForm] = useState(initial)
  const [saving, setSaving] = useState(false)
  const [locating, setLocating] = useState(false)
  const [notice, setNotice] = useState(null)
  const [allUsers, setAllUsers] = useState([])
  const [usersLoading, setUsersLoading] = useState(true)
  const firstRef = useRef(null)

  useEffect(() => { firstRef.current?.focus() }, [])

  useEffect(() => {
    usersApi.list().then((res) => {
      if (res.isSuccess && Array.isArray(res.data)) {
        // Only employees (those that have an employeeId linked)
        setAllUsers(res.data.filter((u) => u.employeeId))
      }
      setUsersLoading(false)
    })
  }, [])

  const set = (k, v) => setForm((f) => ({ ...f, [k]: v }))

  const toggleEmployee = (empId) => {
    setForm((f) => ({
      ...f,
      employeeIds: f.employeeIds.includes(empId)
        ? f.employeeIds.filter((id) => id !== empId)
        : [...f.employeeIds, empId],
    }))
  }

  const useMyLocation = async () => {
    setLocating(true)
    const loc = await getCurrentLocation()
    setLocating(false)
    if (loc) {
      set('latitude', loc.latitude.toFixed(6))
      set('longitude', loc.longitude.toFixed(6))
      setNotice(null)
    } else {
      setNotice('Could not read your location (permission denied or unavailable).')
    }
  }

  const submit = async () => {
    setNotice(null)
    if (!form.name.trim()) { setNotice('Branch name is required.'); return }

    const isPolygon = form.geofenceType === 'polygon'
    const polygon = form.polygon || []
    if (isPolygon && polygon.length < 3) {
      setNotice('Draw a work zone with at least 3 points on the map.'); return
    }

    // Circle branches need an explicit centre; polygon branches derive it from the vertices.
    let lat = parseFloat(form.latitude), lng = parseFloat(form.longitude)
    if (isPolygon) {
      lat = polygon.reduce((s, p) => s + p.latitude, 0) / polygon.length
      lng = polygon.reduce((s, p) => s + p.longitude, 0) / polygon.length
    } else if (Number.isNaN(lat) || Number.isNaN(lng)) {
      setNotice('Enter valid latitude and longitude (or click the map to set the centre).'); return
    }

    setSaving(true)
    const res = await branchApi.set({
      id: form.id || undefined,
      name: form.name.trim(),
      address: form.address.trim(),
      latitude: lat,
      longitude: lng,
      radiusMeters: Number(form.radiusMeters) || 100,
      polygon: isPolygon ? polygon : [],
      isActive: !!form.isActive,
      employeeIds: form.employeeIds,
    })
    setSaving(false)
    if (res.isSuccess) {
      onSave()
    } else {
      setNotice(res.message || 'Could not save branch.')
    }
  }

  // Numeric centre for the map (NaN-safe), so the marker tracks the lat/lng fields.
  const mapCenter = {
    lat: parseFloat(form.latitude),
    lng: parseFloat(form.longitude),
  }

  const handleKey = (e) => { if (e.key === 'Escape') onClose() }

  const isEdit = !!form.id

  return (
    <div className="modalOverlay" onClick={onClose} onKeyDown={handleKey}>
      <div className="modal modal--wide" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 600 }}>
        <div className="modal__header">
          <h3 className="modal__title" style={{ margin: 0 }}>{isEdit ? 'Edit branch' : 'Add branch'}</h3>
          <button className="iconBtn" onClick={onClose}>{Icons.close}</button>
        </div>

        <div style={{ padding: '16px 24px 24px' }}>

        {notice && (
          <p className="alert alert--error" style={{ marginBottom: 14 }}>{notice}</p>
        )}

        <div className="fieldRow" style={{ flexWrap: 'wrap', gap: 12 }}>
          <div className="field" style={{ flex: '1 1 200px' }}>
            <label>Name *</label>
            <input ref={firstRef} className="input" value={form.name} onChange={(e) => set('name', e.target.value)} placeholder="Head office" />
          </div>
          <div className="field" style={{ flex: '1 1 200px' }}>
            <label>Address (optional)</label>
            <input className="input" value={form.address} onChange={(e) => set('address', e.target.value)} />
          </div>
        </div>

        {/* Geofence shape selector */}
        <div className="field" style={{ marginTop: 12 }}>
          <label>Geofence type</label>
          <div className="rangeRow" style={{ gap: 8 }}>
            <button
              type="button"
              className={`chip ${form.geofenceType === 'circle' ? 'chip--active' : ''}`}
              onClick={() => set('geofenceType', 'circle')}
            >
              {form.geofenceType === 'circle' && '✓ '}Circle (radius)
            </button>
            <button
              type="button"
              className={`chip ${form.geofenceType === 'polygon' ? 'chip--active' : ''}`}
              onClick={() => set('geofenceType', 'polygon')}
            >
              {form.geofenceType === 'polygon' && '✓ '}Polygon (custom area)
            </button>
            {form.geofenceType === 'polygon' && (form.polygon?.length > 0) && (
              <button type="button" className="linkBtn" style={{ marginLeft: 'auto' }} onClick={() => set('polygon', [])}>
                Clear &amp; redraw
              </button>
            )}
          </div>
        </div>

        <div className="fieldRow" style={{ flexWrap: 'wrap', gap: 12, marginTop: 8 }}>
          {form.geofenceType === 'circle' ? (
            <>
              <div className="field" style={{ flex: '1 1 140px' }}>
                <label>Latitude *</label>
                <input className="input" value={form.latitude} onChange={(e) => set('latitude', e.target.value)} placeholder="14.5995" />
              </div>
              <div className="field" style={{ flex: '1 1 140px' }}>
                <label>Longitude *</label>
                <input className="input" value={form.longitude} onChange={(e) => set('longitude', e.target.value)} placeholder="120.9842" />
              </div>
              <div className="field" style={{ flex: '0 1 120px' }}>
                <label>Radius (m)</label>
                <input type="number" min="10" className="input" value={form.radiusMeters}
                  onChange={(e) => set('radiusMeters', e.target.value)} />
              </div>
              <div className="field" style={{ flex: '0 0 auto', justifyContent: 'flex-end', paddingTop: 22 }}>
                <button className="btnGhost" style={{ whiteSpace: 'nowrap' }} onClick={useMyLocation} disabled={locating}>
                  {Icons.pin}{locating ? ' Locating…' : ' My location'}
                </button>
              </div>
            </>
          ) : (
            <p className="muted" style={{ fontSize: 13, margin: 0 }}>
              {form.polygon?.length >= 3
                ? `Work zone defined with ${form.polygon.length} points. Drag the vertices on the map to fine-tune.`
                : 'Use the map below to draw the authorized work area.'}
            </p>
          )}
        </div>

        {/* Interactive geofence map */}
        <BranchMap
          geofenceType={form.geofenceType}
          center={mapCenter}
          radius={Number(form.radiusMeters) || 100}
          polygon={form.polygon}
          onCenterChange={(lat, lng) => setForm((f) => ({ ...f, latitude: lat.toFixed(6), longitude: lng.toFixed(6) }))}
          onRadiusChange={(m) => set('radiusMeters', m)}
          onPolygonChange={(pts) => set('polygon', pts)}
        />

        <label className="topbar__user" style={{ gap: 6, marginTop: 8 }}>
          <input type="checkbox" checked={form.isActive} onChange={(e) => set('isActive', e.target.checked)} />
          Active
        </label>

        {/* Employee assignment */}
        <div style={{ marginTop: 16 }}>
          <p style={{ margin: '0 0 8px', fontWeight: 600, fontSize: 13 }}>
            Assigned employees
            {form.employeeIds.length > 0 && (
              <span style={{ marginLeft: 8, fontWeight: 400, color: 'var(--text-secondary)' }}>
                ({form.employeeIds.length} selected)
              </span>
            )}
          </p>
          {usersLoading ? (
            <p style={{ fontSize: 13, color: 'var(--text-muted)' }}>Loading employees…</p>
          ) : allUsers.length === 0 ? (
            <p style={{ fontSize: 13, color: 'var(--text-muted)' }}>No employees found.</p>
          ) : (
            <div style={{
              border: '1px solid var(--border-color)', borderRadius: 6,
              maxHeight: 200, overflowY: 'auto',
            }}>
              {allUsers.map((u) => {
                const checked = form.employeeIds.includes(u.employeeId)
                return (
                  <label
                    key={u.id}
                    style={{
                      display: 'flex', alignItems: 'center', gap: 10,
                      padding: '7px 12px', cursor: 'pointer',
                      borderBottom: '1px solid var(--border-color)',
                      background: checked ? 'color-mix(in srgb, var(--gcp-blue) 8%, transparent)' : 'transparent',
                    }}
                  >
                    <input
                      type="checkbox"
                      checked={checked}
                      onChange={() => toggleEmployee(u.employeeId)}
                      style={{ flexShrink: 0 }}
                    />
                    <span style={{ fontSize: 13, flex: 1 }}>{u.employeeName || u.username}</span>
                    <span style={{ fontSize: 11, color: 'var(--text-muted)' }}>{u.department || u.roleName}</span>
                  </label>
                )
              })}
            </div>
          )}
        </div>

        <div className="modal__actions" style={{ marginTop: 20 }}>
          <button className="btnGhost" onClick={onClose} disabled={saving}>Cancel</button>
          <button className="btnPrimary" onClick={submit} disabled={saving}>
            {saving ? 'Saving…' : isEdit ? 'Update branch' : 'Add branch'}
          </button>
        </div>

        </div>{/* end padding wrapper */}
      </div>
    </div>
  )
}

// Admin/Manager panel: define office branch geofences (centre + radius). A clock-in inside a
// branch radius is recorded as "Office", outside every branch as "Outside".
export default function BranchEditor({ onChanged }) {
  const [branches, setBranches] = useState([])
  const [showAdd, setShowAdd] = useState(false)
  const [editTarget, setEditTarget] = useState(null)
  const [deleteTarget, setDeleteTarget] = useState(null)
  const [errNotice, setErrNotice] = useState(null)

  const load = async () => {
    const res = await branchApi.list(false)
    setBranches(res.isSuccess && Array.isArray(res.data) ? res.data : [])
  }
  useEffect(() => { load() }, [])

  const handleSaved = async () => {
    setShowAdd(false)
    setEditTarget(null)
    await load()
    onChanged?.()
  }

  const openEdit = (b) => {
    setEditTarget({
      id: b.id,
      name: b.name,
      address: b.address || '',
      latitude: String(b.latitude),
      longitude: String(b.longitude),
      radiusMeters: b.radiusMeters,
      isActive: b.isActive,
      employeeIds: (b.assignedEmployees || []).map((e) => e.employeeId),
      geofenceType: b.geofenceType || (b.polygon?.length >= 3 ? 'polygon' : 'circle'),
      polygon: b.polygon || [],
    })
  }

  const confirmDelete = async () => {
    const b = deleteTarget
    const res = await branchApi.remove(b.id)
    setDeleteTarget(null)
    if (res.isSuccess) { await load(); onChanged?.() }
    else setErrNotice(res.message || 'Could not delete branch.')
  }

  return (
    <div className="panel">
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 12 }}>
        <h3 className="panel__title" style={{ margin: 0 }}>Office branches (geofence)</h3>
        <button className="btnPrimary" onClick={() => setShowAdd(true)}>+ Add branch</button>
      </div>

      {errNotice && (
        <p className="alert alert--error" style={{ marginBottom: 14 }}>
          {errNotice}
          <button className="linkBtn" style={{ marginLeft: 8 }} onClick={() => setErrNotice(null)}>×</button>
        </p>
      )}

      <div className="tableScroll">
        <table className="table">
          <thead>
            <tr>
              <th className="th">Name</th>
              <th className="th">Address</th>
              <th className="th">Employees</th>
              <th className="th">Geofence</th>
              <th className="th">Active</th>
              <th className="th" />
            </tr>
          </thead>
          <tbody>
            {branches.length === 0 ? (
              <tr><td className="stateCell" colSpan={6}>No branches yet — click &ldquo;Add branch&rdquo; to create one.</td></tr>
            ) : branches.map((b) => (
              <tr key={b.id} className="row">
                <td className="td" style={{ fontWeight: 500 }}>{b.name}</td>
                <td className="td" style={{ color: 'var(--text-secondary)', fontSize: 12 }}>{b.address || '—'}</td>
                <td className="td">
                  {b.assignedEmployees && b.assignedEmployees.length > 0 ? (
                    <span title={b.assignedEmployees.map((e) => e.fullName).join(', ')}
                      style={{ fontSize: 12, color: 'var(--text-secondary)' }}>
                      {b.assignedEmployees.length} employee{b.assignedEmployees.length !== 1 ? 's' : ''}
                    </span>
                  ) : (
                    <span style={{ fontSize: 12, color: 'var(--text-muted)' }}>—</span>
                  )}
                </td>
                <td className="td" style={{ fontSize: 12, color: 'var(--text-secondary)' }}>
                  {(b.geofenceType === 'polygon' || b.polygon?.length >= 3)
                    ? `Polygon · ${b.polygon?.length || 0} pts`
                    : `Circle · ${b.radiusMeters} m`}
                </td>
                <td className="td">{b.isActive ? 'Yes' : 'No'}</td>
                <td className="td" style={{ textAlign: 'right', whiteSpace: 'nowrap' }}>
                  <button className="linkBtn" onClick={() => openEdit(b)}>Edit</button>
                  <button className="linkBtn" style={{ color: 'var(--gcp-red)' }} onClick={() => setDeleteTarget(b)} title="Delete">
                    {Icons.trash}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showAdd && (
        <BranchFormModal
          initial={{ ...EMPTY }}
          onSave={handleSaved}
          onClose={() => setShowAdd(false)}
        />
      )}

      {editTarget && (
        <BranchFormModal
          initial={editTarget}
          onSave={handleSaved}
          onClose={() => setEditTarget(null)}
        />
      )}

      {deleteTarget && (
        <ConfirmDeleteModal
          title="Delete branch"
          description={`This will permanently remove the "${deleteTarget.name}" geofence. Existing attendance records are not affected.`}
          confirmText={deleteTarget.name}
          onConfirm={confirmDelete}
          onClose={() => setDeleteTarget(null)}
        />
      )}
    </div>
  )
}
