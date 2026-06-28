import { useEffect, useRef, useState } from 'react'
import { licenseApi } from '../../lib/api'

const DURATIONS = [
  { key: '1month', label: '1 Month' },
  { key: '1year', label: '1 Year' },
  { key: '2year', label: '2 Years' },
]

export default function LicenseBanner({ licenseStatus, onActivated }) {
  const [showForm, setShowForm] = useState(false)
  const [mode, setMode] = useState('request') // 'request' | 'key'

  // request flow
  const [duration, setDuration] = useState('1year')
  const [requester, setRequester] = useState('')
  const [requesting, setRequesting] = useState(false)
  const [requested, setRequested] = useState(false)

  // key flow
  const [key, setKey] = useState('')
  const [activating, setActivating] = useState(false)

  const [result, setResult] = useState(null)
  const pollRef = useRef(null)

  // Once a request is sent, poll status so the banner clears when the owner confirms.
  useEffect(() => {
    if (!requested) return
    pollRef.current = setInterval(async () => {
      const s = await licenseApi.status()
      if (s.isSuccess && s.data?.status === 'Licensed') {
        clearInterval(pollRef.current)
        onActivated?.(s.data)
      }
    }, 8000)
    return () => clearInterval(pollRef.current)
  }, [requested, onActivated])

  if (!licenseStatus || licenseStatus.status === 'Licensed') return null

  const isExpired = licenseStatus.status === 'Expired' || licenseStatus.status === 'Invalid'
  const bannerColor = isExpired ? 'var(--gcp-red)' : 'var(--gcp-yellow)'

  const sendRequest = async () => {
    setRequesting(true)
    setResult(null)
    const res = await licenseApi.request(duration, requester.trim() || undefined)
    setRequesting(false)
    if (res.isSuccess || res.data?.success) {
      setRequested(true)
      setResult({ ok: true, msg: res.message || res.data?.message || 'Request sent — confirm from robram3000@gmail.com.' })
    } else {
      setResult({ ok: false, msg: res.message || res.data?.message || 'Could not send request.' })
    }
  }

  const activate = async (e) => {
    e.preventDefault()
    if (!key.trim()) return
    setActivating(true)
    setResult(null)
    const res = await licenseApi.activate(key.trim())
    setActivating(false)
    if (res.isSuccess || res.data?.success) {
      setResult({ ok: true, msg: res.message || res.data?.message || 'License activated.' })
      setKey('')
      setShowForm(false)
      const s = await licenseApi.status()
      if (s.isSuccess) onActivated?.(s.data)
    } else {
      setResult({ ok: false, msg: res.message || 'Activation failed.' })
    }
  }

  const chip = (active) => ({
    padding: '5px 12px', borderRadius: 14, cursor: 'pointer', fontSize: 12, fontWeight: 600,
    border: `1px solid ${active ? 'var(--gcp-blue)' : 'var(--border-color)'}`,
    background: active ? 'color-mix(in srgb, var(--gcp-blue) 14%, transparent)' : 'var(--bg-primary)',
    color: active ? 'var(--gcp-blue)' : 'var(--text-secondary)',
  })

  return (
    <div style={{
      background: `color-mix(in srgb, ${bannerColor} 10%, var(--bg-secondary))`,
      borderBottom: `1px solid ${bannerColor}`,
      padding: '8px 20px', display: 'flex', alignItems: 'center', gap: 12,
      flexWrap: 'wrap', fontSize: 13, zIndex: 100,
    }}>
      <span style={{ color: bannerColor, fontWeight: 600 }}>
        {isExpired ? '⚠ License Expired' : `Trial — ${licenseStatus.trialDaysRemaining} day(s) remaining`}
      </span>
      <span style={{ color: 'var(--text-secondary)' }}>
        {isExpired ? 'API access restricted.' : 'Choose a duration to request a license.'}
      </span>

      <button
        onClick={() => { setShowForm(v => !v); setResult(null); setRequested(false) }}
        style={{ marginLeft: 'auto', padding: '4px 14px', background: 'transparent', border: `1px solid ${bannerColor}`, color: bannerColor, borderRadius: 4, cursor: 'pointer', fontSize: 12, fontWeight: 500 }}
      >
        {showForm ? 'Cancel' : 'Activate License'}
      </button>

      {showForm && (
        <div style={{ width: '100%', marginTop: 6 }}>
          {requested ? (
            /* waiting for owner confirmation */
            <div style={{ display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap' }}>
              <span className="spinner spinner--blue" />
              <span style={{ color: 'var(--text-secondary)', fontSize: 12 }}>
                Activation request sent to <strong style={{ color: 'var(--gcp-blue)' }}>robram3000@gmail.com</strong>.
                Confirm it from that inbox — this unlocks automatically once confirmed.
              </span>
              <button onClick={() => { setRequested(false); setResult(null) }}
                style={{ background: 'none', border: 'none', color: 'var(--text-muted)', fontSize: 12, cursor: 'pointer', textDecoration: 'underline' }}>
                Change duration
              </button>
            </div>
          ) : mode === 'request' ? (
            /* duration picker + request */
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
              <span style={{ color: 'var(--text-secondary)', fontSize: 12 }}>Duration:</span>
              {DURATIONS.map(d => (
                <button key={d.key} type="button" onClick={() => setDuration(d.key)} style={chip(duration === d.key)}>
                  {duration === d.key ? '✓ ' : ''}{d.label}
                </button>
              ))}
              <input
                value={requester}
                onChange={e => setRequester(e.target.value)}
                placeholder="your email (optional)"
                style={{ minWidth: 180, padding: '6px 10px', background: 'var(--bg-primary)', border: '1px solid var(--border-color)', borderRadius: 4, color: 'var(--text-primary)', fontSize: 12 }}
              />
              <button
                type="button"
                onClick={sendRequest}
                disabled={requesting}
                style={{ padding: '6px 16px', background: 'var(--gcp-blue)', border: 'none', borderRadius: 4, color: '#202124', fontWeight: 600, fontSize: 12, cursor: requesting ? 'not-allowed' : 'pointer', opacity: requesting ? 0.5 : 1 }}
              >
                {requesting ? 'Sending…' : `Request ${DURATIONS.find(d => d.key === duration)?.label}`}
              </button>
              <button type="button" onClick={() => { setMode('key'); setResult(null) }}
                style={{ background: 'none', border: 'none', color: 'var(--text-muted)', fontSize: 12, cursor: 'pointer', textDecoration: 'underline' }}>
                Have a key?
              </button>
              {result && <span style={{ color: result.ok ? 'var(--gcp-green)' : 'var(--gcp-red)', fontSize: 12, width: '100%' }}>{result.msg}</span>}
            </div>
          ) : (
            /* paste signed key */
            <form onSubmit={activate} style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
              <input
                value={key}
                onChange={e => { setKey(e.target.value); setResult(null) }}
                placeholder="Paste license key from robram3000@gmail.com…"
                autoFocus
                style={{ flex: 1, minWidth: 280, padding: '6px 10px', background: 'var(--bg-primary)', border: '1px solid var(--border-color)', borderRadius: 4, color: 'var(--text-primary)', fontSize: 12, fontFamily: 'monospace' }}
              />
              <button type="submit" disabled={activating || !key.trim()}
                style={{ padding: '6px 16px', background: 'var(--gcp-blue)', border: 'none', borderRadius: 4, color: '#202124', fontWeight: 600, fontSize: 12, cursor: 'pointer', opacity: activating || !key.trim() ? 0.5 : 1 }}>
                {activating ? 'Activating…' : 'Activate'}
              </button>
              <button type="button" onClick={() => { setMode('request'); setResult(null) }}
                style={{ background: 'none', border: 'none', color: 'var(--text-muted)', fontSize: 12, cursor: 'pointer', textDecoration: 'underline' }}>
                Back to durations
              </button>
              {result && <span style={{ color: result.ok ? 'var(--gcp-green)' : 'var(--gcp-red)', fontSize: 12 }}>{result.msg}</span>}
            </form>
          )}
        </div>
      )}
    </div>
  )
}
