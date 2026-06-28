import { useEffect, useRef, useState } from 'react'
import { licenseApi } from '../../lib/api'

const DURATIONS = [
  { key: '1month', label: '1 Month' },
  { key: '1year', label: '1 Year' },
  { key: '2year', label: '2 Years' },
]

export default function LicenseGate({ licenseStatus, onActivated, onSignOut }) {
  // ── Duration-request flow ──────────────────────────────────────────
  const [duration, setDuration] = useState('1year')
  const [requester, setRequester] = useState('')
  const [requesting, setRequesting] = useState(false)
  const [requested, setRequested] = useState(false)
  const [reqMsg, setReqMsg] = useState('')
  const [reqError, setReqError] = useState('')

  // ── Manual key flow (secondary) ────────────────────────────────────
  const [showKey, setShowKey] = useState(false)
  const [key, setKey] = useState('')
  const [activating, setActivating] = useState(false)
  const [error, setError] = useState('')

  const pollRef = useRef(null)

  // After a request is sent, poll status so the gate clears automatically once the owner confirms.
  useEffect(() => {
    if (!requested) return
    pollRef.current = setInterval(async () => {
      const s = await licenseApi.status()
      if (s.isSuccess && s.data?.status !== 'Expired' && s.data?.status !== 'Invalid') {
        clearInterval(pollRef.current)
        onActivated?.(s.data)
      }
    }, 8000)
    return () => clearInterval(pollRef.current)
  }, [requested, onActivated])

  const sendRequest = async () => {
    setRequesting(true)
    setReqError('')
    const res = await licenseApi.request(duration, requester.trim() || undefined)
    setRequesting(false)
    if (res.isSuccess || res.data?.success) {
      setRequested(true)
      setReqMsg(res.message || res.data?.message || 'Request sent. Waiting for confirmation…')
    } else {
      setReqError(res.message || res.data?.message || 'Could not send the request. Try again.')
    }
  }

  const activate = async (e) => {
    e.preventDefault()
    if (!key.trim()) return
    setActivating(true)
    setError('')
    const res = await licenseApi.activate(key.trim())
    setActivating(false)
    if (res.isSuccess || res.data?.success) {
      const s = await licenseApi.status()
      if (s.isSuccess) onActivated?.(s.data)
    } else {
      setError(res.message || res.data?.message || 'Invalid or expired license key.')
    }
  }

  const expiredDate = licenseStatus?.expiryDate
    ? new Date(licenseStatus.expiryDate).toLocaleDateString()
    : null

  return (
    <div style={{ minHeight: '100vh', background: 'var(--bg-primary)', display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 24 }}>
      <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: 8, padding: 40, maxWidth: 520, width: '100%', boxShadow: 'var(--shadow-lg)' }}>
        <div style={{ textAlign: 'center', marginBottom: 28 }}>
          <div style={{ width: 52, height: 52, borderRadius: '50%', background: 'color-mix(in srgb, var(--gcp-red) 15%, transparent)', display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 16px', fontSize: 24 }}>
            ⚠
          </div>
          <h2 style={{ margin: '0 0 8px', color: 'var(--text-primary)', fontSize: 20, fontWeight: 600 }}>License Required</h2>
          <p style={{ margin: 0, color: 'var(--text-secondary)', fontSize: 14, lineHeight: 1.5 }}>
            {expiredDate ? `This deployment's license expired on ${expiredDate}.` : 'The 30-day trial for this deployment has ended.'}
            {' '}Choose a license duration to request access.
          </p>
        </div>

        {requested ? (
          /* ── Waiting for owner confirmation ── */
          <div style={{ textAlign: 'center', padding: '8px 0 4px' }}>
            <div style={{ display: 'inline-flex', alignItems: 'center', gap: 10, color: 'var(--text-secondary)', fontSize: 14, marginBottom: 10 }}>
              <span className="spinner spinner--blue" /> Waiting for confirmation…
            </div>
            <p style={{ color: 'var(--text-secondary)', fontSize: 13, lineHeight: 1.5, margin: '0 0 4px' }}>{reqMsg}</p>
            <p style={{ color: 'var(--text-muted)', fontSize: 12, lineHeight: 1.5 }}>
              An activation request was emailed to <strong style={{ color: 'var(--gcp-blue)' }}>robram3000@gmail.com</strong>.
              Once it’s confirmed, this screen unlocks automatically.
            </p>
            <button onClick={() => { setRequested(false); setReqMsg('') }}
              style={{ marginTop: 10, background: 'none', border: 'none', color: 'var(--text-muted)', fontSize: 12, cursor: 'pointer', textDecoration: 'underline' }}>
              Choose a different duration
            </button>
          </div>
        ) : (
          /* ── Duration picker ── */
          <>
            <label style={{ fontSize: 12, color: 'var(--text-secondary)', display: 'block', marginBottom: 8 }}>License duration</label>
            <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
              {DURATIONS.map((d) => (
                <button
                  key={d.key}
                  type="button"
                  onClick={() => setDuration(d.key)}
                  style={{
                    flex: 1, padding: '12px 8px', borderRadius: 6, cursor: 'pointer', fontSize: 13, fontWeight: 600,
                    border: `1px solid ${duration === d.key ? 'var(--gcp-blue)' : 'var(--border-color)'}`,
                    background: duration === d.key ? 'color-mix(in srgb, var(--gcp-blue) 12%, transparent)' : 'var(--bg-primary)',
                    color: duration === d.key ? 'var(--gcp-blue)' : 'var(--text-secondary)',
                  }}
                >
                  {d.label}
                </button>
              ))}
            </div>

            <label style={{ fontSize: 12, color: 'var(--text-secondary)', display: 'block', marginBottom: 6 }}>Your email / organization (optional)</label>
            <input
              value={requester}
              onChange={(e) => setRequester(e.target.value)}
              placeholder="e.g. hr@company.com"
              style={{ width: '100%', padding: '10px 12px', background: 'var(--bg-primary)', border: '1px solid var(--border-color)', borderRadius: 4, color: 'var(--text-primary)', fontSize: 13, outline: 'none', marginBottom: 16 }}
            />

            {reqError && <p style={{ color: 'var(--gcp-red)', fontSize: 12, margin: '0 0 12px' }}>{reqError}</p>}

            <button
              type="button"
              onClick={sendRequest}
              disabled={requesting}
              style={{ width: '100%', padding: '10px', background: 'var(--gcp-blue)', border: 'none', borderRadius: 4, color: '#202124', fontWeight: 600, fontSize: 14, cursor: requesting ? 'not-allowed' : 'pointer', opacity: requesting ? 0.55 : 1 }}
            >
              {requesting ? 'Sending request…' : `Request ${DURATIONS.find((d) => d.key === duration)?.label} license`}
            </button>

            {/* Secondary: manual signed key */}
            <div style={{ marginTop: 18, borderTop: '1px solid var(--border-color)', paddingTop: 14 }}>
              <button onClick={() => setShowKey((v) => !v)}
                style={{ background: 'none', border: 'none', color: 'var(--text-muted)', fontSize: 12, cursor: 'pointer', textDecoration: 'underline' }}>
                {showKey ? 'Hide' : 'Already have a license key? Paste it'}
              </button>
              {showKey && (
                <form onSubmit={activate} style={{ marginTop: 12 }}>
                  <textarea
                    value={key}
                    onChange={(e) => { setKey(e.target.value); setError('') }}
                    placeholder="Paste your license key here…"
                    rows={3}
                    style={{ width: '100%', padding: '10px 12px', background: 'var(--bg-primary)', border: `1px solid ${error ? 'var(--gcp-red)' : 'var(--border-color)'}`, borderRadius: 4, color: 'var(--text-primary)', fontSize: 12, fontFamily: 'monospace', resize: 'vertical', outline: 'none' }}
                  />
                  {error && <p style={{ color: 'var(--gcp-red)', fontSize: 12, margin: '6px 0 0' }}>{error}</p>}
                  <button type="submit" disabled={activating || !key.trim()}
                    style={{ marginTop: 10, width: '100%', padding: '9px', background: 'transparent', border: '1px solid var(--gcp-blue)', borderRadius: 4, color: 'var(--gcp-blue)', fontWeight: 600, fontSize: 13, cursor: activating || !key.trim() ? 'not-allowed' : 'pointer', opacity: activating || !key.trim() ? 0.55 : 1 }}>
                    {activating ? 'Verifying…' : 'Activate key'}
                  </button>
                </form>
              )}
            </div>
          </>
        )}

        <div style={{ marginTop: 20, textAlign: 'center' }}>
          <button onClick={onSignOut}
            style={{ background: 'none', border: 'none', color: 'var(--text-muted)', fontSize: 12, cursor: 'pointer', textDecoration: 'underline' }}>
            Sign out
          </button>
        </div>
      </div>
    </div>
  )
}
