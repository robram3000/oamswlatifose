import { useEffect, useRef, useState } from 'react'
import { loadGoogleMaps, isMapsConfigured } from '../../lib/maps'
import { branchApi } from '../../lib/api'

const DEFAULT_CENTER = { lat: 14.5995, lng: 120.9842 }

// Marker colour by where the clock-in was resolved.
const PIN = {
  Office: '#34a853',   // green — inside a branch geofence
  Outside: '#ea4335',  // red — off-site
  Unknown: '#9aa0a6',  // grey — no GPS captured
}

/**
 * Read-only monitoring map: plots each attendance record's clock-in location as a coloured pin
 * (Office / Outside / Unknown) and overlays every active branch geofence (circle or polygon).
 * Gives admins the map-based visualization of verified work locations.
 */
export default function AttendanceMap({ rows, dateLabel }) {
  const divRef = useRef(null)
  const gRef = useRef(null)
  const mapRef = useRef(null)
  const overlaysRef = useRef([])
  const infoRef = useRef(null)
  const [status, setStatus] = useState(() => (isMapsConfigured() ? 'loading' : 'error')) // loading | ready | error
  const [errMsg, setErrMsg] = useState(() => (isMapsConfigured() ? '' : 'Google Maps API key not configured.'))
  const [branches, setBranches] = useState([])

  const located = (rows || []).filter(
    (r) => Number.isFinite(r.latitude) && Number.isFinite(r.longitude),
  )

  // Fetch active branch geofences once.
  useEffect(() => {
    branchApi.list(true).then((res) => {
      if (res.isSuccess && Array.isArray(res.data)) setBranches(res.data)
    })
  }, [])

  // One-time map init.
  useEffect(() => {
    if (!isMapsConfigured()) return
    let cancelled = false
    loadGoogleMaps()
      .then((google) => {
        if (cancelled || !divRef.current) return
        gRef.current = google
        mapRef.current = new google.maps.Map(divRef.current, {
          center: DEFAULT_CENTER, zoom: 12,
          mapTypeControl: false, streetViewControl: false, fullscreenControl: false,
        })
        infoRef.current = new google.maps.InfoWindow()
        setStatus('ready')
      })
      .catch((e) => { if (!cancelled) { setStatus('error'); setErrMsg(e.message) } })
    return () => { cancelled = true }
  }, [])

  // Redraw overlays whenever data changes.
  useEffect(() => {
    if (status !== 'ready') return
    const google = gRef.current
    const map = mapRef.current

    overlaysRef.current.forEach((o) => o.setMap(null))
    overlaysRef.current = []
    const bounds = new google.maps.LatLngBounds()
    let any = false

    // Branch geofences first (drawn under the pins).
    branches.forEach((b) => {
      if (b.geofenceType === 'polygon' && Array.isArray(b.polygon) && b.polygon.length >= 3) {
        const path = b.polygon.map((p) => ({ lat: p.latitude, lng: p.longitude }))
        const poly = new google.maps.Polygon({
          map, paths: path, clickable: false,
          fillColor: '#1a73e8', fillOpacity: 0.08, strokeColor: '#1a73e8', strokeWeight: 1.5,
        })
        overlaysRef.current.push(poly)
        path.forEach((ll) => { bounds.extend(ll); any = true })
      } else if (Number.isFinite(b.latitude) && Number.isFinite(b.longitude)) {
        const circle = new google.maps.Circle({
          map, center: { lat: b.latitude, lng: b.longitude }, radius: b.radiusMeters || 100,
          clickable: false, fillColor: '#1a73e8', fillOpacity: 0.08, strokeColor: '#1a73e8', strokeWeight: 1.5,
        })
        overlaysRef.current.push(circle)
        bounds.union(circle.getBounds()); any = true
      }
    })

    // Attendance pins.
    located.forEach((r) => {
      const pos = { lat: r.latitude, lng: r.longitude }
      const color = PIN[r.workLocation] || PIN.Unknown
      const marker = new google.maps.Marker({
        map, position: pos, title: r.employeeName,
        icon: {
          path: google.maps.SymbolPath.CIRCLE,
          fillColor: color, fillOpacity: 1, strokeColor: '#fff', strokeWeight: 2, scale: 7,
        },
      })
      marker.addListener('click', () => {
        infoRef.current.setContent(
          `<div style="font:13px system-ui;min-width:150px">
             <strong>${escapeHtml(r.employeeName || 'Employee')}</strong><br/>
             ${escapeHtml(r.workLocation || 'Unknown')}${r.branchName ? ' · ' + escapeHtml(r.branchName) : ''}<br/>
             ${r.timeInFormatted ? 'In: ' + escapeHtml(r.timeInFormatted) : ''}${r.timeOutFormatted ? ' · Out: ' + escapeHtml(r.timeOutFormatted) : ''}<br/>
             <span style="color:#666">${escapeHtml(r.status || '')}</span>
           </div>`,
        )
        infoRef.current.open(map, marker)
      })
      overlaysRef.current.push(marker)
      bounds.extend(pos); any = true
    })

    if (any && !bounds.isEmpty()) {
      map.fitBounds(bounds)
      // Avoid over-zooming when there is a single point.
      const once = google.maps.event.addListenerOnce(map, 'idle', () => {
        if (map.getZoom() > 17) map.setZoom(17)
      })
      overlaysRef.current.push({ setMap: () => google.maps.event.removeListener(once) })
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [status, branches, rows])

  const counts = located.reduce((a, r) => {
    const k = r.workLocation || 'Unknown'
    a[k] = (a[k] || 0) + 1
    return a
  }, {})

  return (
    <div className="panel" style={{ marginTop: 16 }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 8, marginBottom: 10 }}>
        <div>
          <h3 className="panel__title" style={{ margin: 0 }}>Location map{dateLabel ? ` · ${dateLabel}` : ''}</h3>
          <p className="pageSub" style={{ marginTop: 2 }}>Verified clock-in locations and office geofences.</p>
        </div>
        <div style={{ display: 'flex', gap: 14, fontSize: 12, color: 'var(--text-secondary)' }}>
          {[['Office', PIN.Office], ['Outside', PIN.Outside], ['Unknown', PIN.Unknown]].map(([label, c]) => (
            <span key={label} style={{ display: 'flex', alignItems: 'center', gap: 5 }}>
              <span style={{ width: 10, height: 10, borderRadius: '50%', background: c, border: '1px solid #fff', boxShadow: '0 0 0 1px #0002' }} />
              {label}{counts[label] ? ` (${counts[label]})` : ''}
            </span>
          ))}
        </div>
      </div>

      <div
        ref={divRef}
        style={{ width: '100%', height: 320, borderRadius: 8, border: '1px solid var(--border-color)', background: 'var(--surface)' }}
      />

      {status === 'loading' && <p className="muted" style={{ fontSize: 12, marginTop: 6 }}>Loading map…</p>}
      {status === 'error' && (
        <p className="alert alert--error" style={{ fontSize: 12, marginTop: 6 }}>{errMsg}</p>
      )}
      {status === 'ready' && located.length === 0 && (
        <p className="muted" style={{ fontSize: 12, marginTop: 6 }}>
          No GPS-tagged clock-ins for this date yet — pins appear once employees clock in with location enabled.
        </p>
      )}
    </div>
  )
}

function escapeHtml(s) {
  return String(s).replace(/[&<>"']/g, (c) => (
    { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]
  ))
}
