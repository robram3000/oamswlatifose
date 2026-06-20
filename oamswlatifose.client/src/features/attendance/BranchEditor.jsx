import { useEffect, useState } from 'react'
import { branchApi } from '../../lib/api'
import { getCurrentLocation } from '../../lib/geo'
import { Icons } from '../../lib/ui'

const EMPTY = { id: null, name: '', address: '', latitude: '', longitude: '', radiusMeters: 100, isActive: true }

// Admin/Manager panel: define office branch geofences (centre + radius). A clock-in inside a
// branch radius is recorded as "Office", outside every branch as "Outside".
export default function BranchEditor({ onChanged }) {
  const [branches, setBranches] = useState([])
  const [form, setForm] = useState(EMPTY)
  const [saving, setSaving] = useState(false)
  const [locating, setLocating] = useState(false)
  const [notice, setNotice] = useState(null)

  const load = async () => {
    const res = await branchApi.list(false)
    setBranches(res.isSuccess && Array.isArray(res.data) ? res.data : [])
  }
  useEffect(() => { load() }, [])

  const set = (k, v) => setForm((f) => ({ ...f, [k]: v }))
  const reset = () => setForm(EMPTY)

  const useMyLocation = async () => {
    setLocating(true)
    const loc = await getCurrentLocation()
    setLocating(false)
    if (loc) {
      set('latitude', loc.latitude.toFixed(6))
      set('longitude', loc.longitude.toFixed(6))
      setNotice(null)
    } else {
      setNotice({ type: 'error', text: 'Could not read your location (permission denied or unavailable).' })
    }
  }

  const save = async () => {
    setNotice(null)
    if (!form.name.trim()) { setNotice({ type: 'error', text: 'Branch name is required.' }); return }
    const lat = parseFloat(form.latitude), lng = parseFloat(form.longitude)
    if (Number.isNaN(lat) || Number.isNaN(lng)) { setNotice({ type: 'error', text: 'Enter valid latitude and longitude.' }); return }
    setSaving(true)
    const res = await branchApi.set({
      id: form.id || undefined,
      name: form.name.trim(),
      address: form.address.trim(),
      latitude: lat,
      longitude: lng,
      radiusMeters: Number(form.radiusMeters) || 100,
      isActive: !!form.isActive,
    })
    setSaving(false)
    if (res.isSuccess) {
      setNotice({ type: 'ok', text: 'Branch saved.' })
      reset(); await load(); onChanged?.()
    } else {
      setNotice({ type: 'error', text: res.message || 'Could not save branch.' })
    }
  }

  const edit = (b) => setForm({
    id: b.id, name: b.name, address: b.address || '',
    latitude: String(b.latitude), longitude: String(b.longitude),
    radiusMeters: b.radiusMeters, isActive: b.isActive,
  })

  const remove = async (b) => {
    const res = await branchApi.remove(b.id)
    if (res.isSuccess) { if (form.id === b.id) reset(); await load(); onChanged?.() }
    else setNotice({ type: 'error', text: res.message || 'Could not delete branch.' })
  }

  return (
    <div className="panel">
      <h3 className="panel__title">Office branches (geofence)</h3>

      {notice && (
        <p className={`alert ${notice.type === 'ok' ? 'alert--ok' : 'alert--error'}`} style={{ marginBottom: 14 }}>
          {notice.text}
        </p>
      )}

      {/* Existing branches */}
      <div className="tableScroll" style={{ marginBottom: 16 }}>
        <table className="table">
          <thead>
            <tr>
              <th className="th">Name</th>
              <th className="th">Latitude</th>
              <th className="th">Longitude</th>
              <th className="th thNum">Radius (m)</th>
              <th className="th">Active</th>
              <th className="th" />
            </tr>
          </thead>
          <tbody>
            {branches.length === 0 ? (
              <tr><td className="stateCell" colSpan={6}>No branches yet — add one below.</td></tr>
            ) : branches.map((b) => (
              <tr key={b.id} className="row">
                <td className="td">{b.name}</td>
                <td className="td">{b.latitude.toFixed(5)}</td>
                <td className="td">{b.longitude.toFixed(5)}</td>
                <td className="td tdNum">{b.radiusMeters}</td>
                <td className="td">{b.isActive ? 'Yes' : 'No'}</td>
                <td className="td" style={{ textAlign: 'right' }}>
                  <button className="linkBtn" onClick={() => edit(b)}>Edit</button>
                  <button className="linkBtn" style={{ color: 'var(--gcp-red)' }} onClick={() => remove(b)} title="Delete">{Icons.trash}</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Add / edit form */}
      <div className="fieldRow">
        <div className="field" style={{ minWidth: 200 }}>
          <label htmlFor="bn">Name</label>
          <input id="bn" className="input" value={form.name} onChange={(e) => set('name', e.target.value)} placeholder="Head office" />
        </div>
        <div className="field" style={{ minWidth: 200 }}>
          <label htmlFor="ba">Address (optional)</label>
          <input id="ba" className="input" value={form.address} onChange={(e) => set('address', e.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="blat">Latitude</label>
          <input id="blat" className="input" value={form.latitude} onChange={(e) => set('latitude', e.target.value)} placeholder="14.5995" />
        </div>
        <div className="field">
          <label htmlFor="blng">Longitude</label>
          <input id="blng" className="input" value={form.longitude} onChange={(e) => set('longitude', e.target.value)} placeholder="120.9842" />
        </div>
        <div className="field">
          <label htmlFor="brad">Radius (m)</label>
          <input id="brad" type="number" min="10" className="input" style={{ minWidth: 100 }}
                 value={form.radiusMeters} onChange={(e) => set('radiusMeters', e.target.value)} />
        </div>
      </div>

      <div className="actions">
        <button className="btnGhost" onClick={useMyLocation} disabled={locating}>
          {Icons.pin}{locating ? 'Locating…' : 'Use my current location'}
        </button>
        <label className="topbar__user" style={{ gap: 6 }}>
          <input type="checkbox" checked={form.isActive} onChange={(e) => set('isActive', e.target.checked)} /> Active
        </label>
        <button className="btnPrimary" onClick={save} disabled={saving}>
          {saving ? 'Saving…' : form.id ? 'Update branch' : 'Add branch'}
        </button>
        {form.id && <button className="btnGhost" onClick={reset}>Cancel edit</button>}
      </div>
    </div>
  )
}
