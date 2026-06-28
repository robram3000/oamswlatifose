import { useEffect, useRef, useState } from 'react'
import { loadGoogleMaps, isMapsConfigured } from '../../lib/maps'

// Default view: Metro Manila — only used until the user picks a point/branch location.
const DEFAULT_CENTER = { lat: 14.5995, lng: 120.9842 }

const pathToPoints = (path) =>
  path.getArray().map((ll) => ({ latitude: ll.lat(), longitude: ll.lng() }))

/**
 * Interactive geofence editor backed by Google Maps.
 *  - "circle"  : a draggable centre marker + an editable-radius circle. Map clicks move the centre.
 *  - "polygon" : draw a polygon (Drawing Manager); once drawn the vertices are editable/draggable.
 *
 * The map is the source of truth for geometry once loaded; it reports changes up through the
 * callbacks. `center`/`radius` are also honoured as external nudges (e.g. the "My location"
 * button or the radius number field), and clearing `polygon` to empty re-opens the draw tool.
 */
export default function BranchMap({
  geofenceType, center, radius, polygon,
  onCenterChange, onRadiusChange, onPolygonChange,
}) {
  const divRef = useRef(null)
  const gRef = useRef(null)         // google namespace
  const mapRef = useRef(null)
  const markerRef = useRef(null)
  const circleRef = useRef(null)
  const polyRef = useRef(null)
  const dmRef = useRef(null)

  // Latest callbacks, so map listeners (bound once) always call the current handlers.
  const cb = useRef({})
  cb.current = { onCenterChange, onRadiusChange, onPolygonChange }

  // Track the last external primitive values we applied, to tell "user edited the map"
  // apart from "a prop changed" and avoid feedback loops.
  const applied = useRef({ lat: null, lng: null, radius: null, polyLen: null })

  const [status, setStatus] = useState(() => (isMapsConfigured() ? 'loading' : 'error')) // loading | ready | error
  const [errMsg, setErrMsg] = useState(() => (isMapsConfigured() ? '' : 'Google Maps API key not configured.'))

  // ── One-time map init ──────────────────────────────────────────────
  useEffect(() => {
    if (!isMapsConfigured()) return
    let cancelled = false
    loadGoogleMaps()
      .then((google) => {
        if (cancelled || !divRef.current) return
        gRef.current = google
        const start = (Number.isFinite(center?.lat) && Number.isFinite(center?.lng)) ? center : DEFAULT_CENTER
        mapRef.current = new google.maps.Map(divRef.current, {
          center: start,
          zoom: 16,
          mapTypeControl: false,
          streetViewControl: false,
          fullscreenControl: false,
        })
        setStatus('ready')
      })
      .catch((e) => { if (!cancelled) { setStatus('error'); setErrMsg(e.message) } })
    return () => { cancelled = true }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  // ── Build/teardown tools whenever the shape mode changes ───────────
  useEffect(() => {
    if (status !== 'ready') return
    const map = mapRef.current
    teardownAll()

    if (geofenceType === 'polygon') {
      const pts = polygon || []
      if (pts.length >= 3) buildPolygon(pts)
      else enableDrawing()
    } else {
      const c = (Number.isFinite(center?.lat) && Number.isFinite(center?.lng)) ? center : map.getCenter().toJSON()
      buildCircle(c, Number(radius) || 100)
    }
    return teardownAll
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [status, geofenceType])

  // ── External nudges: recenter circle on a new center prop ──────────
  useEffect(() => {
    if (status !== 'ready' || geofenceType !== 'circle') return
    if (!Number.isFinite(center?.lat) || !Number.isFinite(center?.lng)) return
    if (center.lat === applied.current.lat && center.lng === applied.current.lng) return
    applied.current.lat = center.lat
    applied.current.lng = center.lng
    const ll = new gRef.current.maps.LatLng(center.lat, center.lng)
    markerRef.current?.setPosition(ll)
    circleRef.current?.setCenter(ll)
    mapRef.current?.panTo(ll)
  }, [center?.lat, center?.lng, status, geofenceType])

  // ── External nudge: radius field changed ──────────────────────────
  useEffect(() => {
    if (status !== 'ready' || geofenceType !== 'circle' || !circleRef.current) return
    const r = Number(radius) || 100
    if (r === applied.current.radius) return
    applied.current.radius = r
    if (Math.round(circleRef.current.getRadius()) !== r) circleRef.current.setRadius(r)
  }, [radius, status, geofenceType])

  // ── External nudge: polygon cleared → reopen the draw tool ─────────
  useEffect(() => {
    if (status !== 'ready' || geofenceType !== 'polygon') return
    const len = (polygon || []).length
    if (len === 0 && polyRef.current) {
      teardownShapes()
      enableDrawing()
    }
    applied.current.polyLen = len
  }, [polygon, status, geofenceType])

  // ── builders ───────────────────────────────────────────────────────
  function buildCircle(c, r) {
    const google = gRef.current
    const map = mapRef.current
    const center0 = new google.maps.LatLng(c.lat, c.lng)
    applied.current.lat = c.lat; applied.current.lng = c.lng; applied.current.radius = r

    markerRef.current = new google.maps.Marker({ position: center0, map, draggable: true })
    circleRef.current = new google.maps.Circle({
      map, center: center0, radius: r, editable: true, draggable: false,
      fillColor: '#1a73e8', fillOpacity: 0.12, strokeColor: '#1a73e8', strokeWeight: 2,
    })

    const pushCenter = (ll) => {
      applied.current.lat = ll.lat(); applied.current.lng = ll.lng()
      cb.current.onCenterChange?.(ll.lat(), ll.lng())
    }
    markerRef.current.addListener('dragend', (e) => { circleRef.current.setCenter(e.latLng); pushCenter(e.latLng) })
    map.addListener('click', (e) => {
      markerRef.current.setPosition(e.latLng); circleRef.current.setCenter(e.latLng); pushCenter(e.latLng)
    })
    circleRef.current.addListener('radius_changed', () => {
      const rr = Math.round(circleRef.current.getRadius())
      applied.current.radius = rr
      cb.current.onRadiusChange?.(rr)
    })
    circleRef.current.addListener('center_changed', () => {
      const cc = circleRef.current.getCenter()
      markerRef.current.setPosition(cc)
    })
  }

  function buildPolygon(points) {
    const google = gRef.current
    const map = mapRef.current
    const path = points.map((p) => ({ lat: p.latitude, lng: p.longitude }))
    polyRef.current = new google.maps.Polygon({
      map, paths: path, editable: true, draggable: true,
      fillColor: '#1a73e8', fillOpacity: 0.12, strokeColor: '#1a73e8', strokeWeight: 2,
    })
    fitToPath(polyRef.current.getPath())
    const report = () => cb.current.onPolygonChange?.(pathToPoints(polyRef.current.getPath()))
    const p = polyRef.current.getPath()
    p.addListener('set_at', report)
    p.addListener('insert_at', report)
    p.addListener('remove_at', report)
    polyRef.current.addListener('dragend', report)
  }

  function enableDrawing() {
    const google = gRef.current
    const map = mapRef.current
    dmRef.current = new google.maps.drawing.DrawingManager({
      drawingMode: google.maps.drawing.OverlayType.POLYGON,
      drawingControl: false,
      polygonOptions: {
        editable: true, draggable: true,
        fillColor: '#1a73e8', fillOpacity: 0.12, strokeColor: '#1a73e8', strokeWeight: 2,
      },
    })
    dmRef.current.setMap(map)
    google.maps.event.addListener(dmRef.current, 'polygoncomplete', (poly) => {
      dmRef.current.setDrawingMode(null)
      dmRef.current.setMap(null)
      dmRef.current = null
      polyRef.current = poly
      const report = () => cb.current.onPolygonChange?.(pathToPoints(poly.getPath()))
      const path = poly.getPath()
      path.addListener('set_at', report)
      path.addListener('insert_at', report)
      path.addListener('remove_at', report)
      poly.addListener('dragend', report)
      report()
    })
  }

  function fitToPath(path) {
    const google = gRef.current
    const bounds = new google.maps.LatLngBounds()
    path.getArray().forEach((ll) => bounds.extend(ll))
    if (!bounds.isEmpty()) mapRef.current.fitBounds(bounds)
  }

  function teardownShapes() {
    markerRef.current?.setMap(null); markerRef.current = null
    circleRef.current?.setMap(null); circleRef.current = null
    polyRef.current?.setMap(null); polyRef.current = null
  }
  function teardownAll() {
    teardownShapes()
    if (dmRef.current) { dmRef.current.setMap(null); dmRef.current = null }
    if (gRef.current && mapRef.current) gRef.current.maps.event.clearListeners(mapRef.current, 'click')
  }

  return (
    <div style={{ marginTop: 8 }}>
      <div
        ref={divRef}
        style={{ width: '100%', height: 260, borderRadius: 8, border: '1px solid var(--border-color)', background: 'var(--surface)' }}
      />
      {status === 'loading' && <p className="muted" style={{ fontSize: 12, marginTop: 6 }}>Loading map…</p>}
      {status === 'error' && (
        <p className="alert alert--error" style={{ fontSize: 12, marginTop: 6 }}>
          {errMsg} You can still set the geofence using the latitude/longitude fields below.
        </p>
      )}
      {status === 'ready' && (
        <p className="muted" style={{ fontSize: 12, marginTop: 6 }}>
          {geofenceType === 'polygon'
            ? (polygon?.length >= 3
                ? 'Drag the vertices to adjust the zone, or drag the whole shape to move it.'
                : 'Click on the map to drop points and close the polygon to define the work zone.')
            : 'Click the map to set the centre, drag the marker to move it, or drag the circle edge to resize the radius.'}
        </p>
      )}
    </div>
  )
}
