import { useState } from 'react'
import { licenseApi } from '../../lib/api'

export default function LicenseGate({ licenseStatus, onActivated, onSignOut }) {
  const [key, setKey] = useState('')
  const [activating, setActivating] = useState(false)
  const [error, setError] = useState('')

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
    <div style={{
      minHeight: '100vh',
      background: 'var(--bg-primary)',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      padding: 24,
    }}>
      <div style={{
        background: 'var(--bg-card)',
        border: '1px solid var(--border-color)',
        borderRadius: 8,
        padding: 40,
        maxWidth: 520,
        width: '100%',
        boxShadow: 'var(--shadow-lg)',
      }}>
        <div style={{ textAlign: 'center', marginBottom: 28 }}>
          <div style={{
            width: 52, height: 52,
            borderRadius: '50%',
            background: 'color-mix(in srgb, var(--gcp-red) 15%, transparent)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            margin: '0 auto 16px',
            fontSize: 24,
          }}>
            ⚠
          </div>
          <h2 style={{ margin: '0 0 8px', color: 'var(--text-primary)', fontSize: 20, fontWeight: 600 }}>
            License Required
          </h2>
          <p style={{ margin: 0, color: 'var(--text-secondary)', fontSize: 14, lineHeight: 1.5 }}>
            {expiredDate
              ? `This deployment's license expired on ${expiredDate}.`
              : 'The 30-day trial for this deployment has ended.'}
            {' '}Access is suspended until a valid license key is entered.
          </p>
        </div>

        <div style={{
          background: 'color-mix(in srgb, var(--gcp-blue) 8%, transparent)',
          border: '1px solid color-mix(in srgb, var(--gcp-blue) 30%, transparent)',
          borderRadius: 6,
          padding: '12px 16px',
          marginBottom: 24,
          fontSize: 13,
          color: 'var(--text-secondary)',
          lineHeight: 1.5,
        }}>
          To obtain a license key, contact{' '}
          <strong style={{ color: 'var(--gcp-blue)' }}>robram3000@gmail.com</strong>
          {' '}with your deployment details. Keys are issued by the system owner only.
        </div>

        <form onSubmit={activate}>
          <label style={{ fontSize: 12, color: 'var(--text-secondary)', display: 'block', marginBottom: 6 }}>
            License Key
          </label>
          <textarea
            value={key}
            onChange={e => { setKey(e.target.value); setError('') }}
            placeholder="Paste your license key here…"
            rows={4}
            style={{
              width: '100%',
              padding: '10px 12px',
              background: 'var(--bg-primary)',
              border: `1px solid ${error ? 'var(--gcp-red)' : 'var(--border-color)'}`,
              borderRadius: 4,
              color: 'var(--text-primary)',
              fontSize: 12,
              fontFamily: 'monospace',
              resize: 'vertical',
              outline: 'none',
            }}
          />

          {error && (
            <p style={{ color: 'var(--gcp-red)', fontSize: 12, margin: '6px 0 0' }}>{error}</p>
          )}

          <button
            type="submit"
            disabled={activating || !key.trim()}
            style={{
              marginTop: 16,
              width: '100%',
              padding: '10px',
              background: 'var(--gcp-blue)',
              border: 'none',
              borderRadius: 4,
              color: '#202124',
              fontWeight: 600,
              fontSize: 14,
              cursor: activating || !key.trim() ? 'not-allowed' : 'pointer',
              opacity: activating || !key.trim() ? 0.55 : 1,
              transition: 'opacity 0.15s',
            }}
          >
            {activating ? 'Verifying…' : 'Activate License'}
          </button>
        </form>

        <div style={{ marginTop: 20, textAlign: 'center' }}>
          <button
            onClick={onSignOut}
            style={{
              background: 'none',
              border: 'none',
              color: 'var(--text-muted)',
              fontSize: 12,
              cursor: 'pointer',
              textDecoration: 'underline',
            }}
          >
            Sign out
          </button>
        </div>
      </div>
    </div>
  )
}
