import { useState } from 'react'
import { licenseApi } from '../../lib/api'

export default function LicenseBanner({ licenseStatus, onActivated }) {
  const [key, setKey] = useState('')
  const [activating, setActivating] = useState(false)
  const [result, setResult] = useState(null)
  const [showForm, setShowForm] = useState(false)

  if (!licenseStatus || licenseStatus.status === 'Licensed') return null

  const isExpired = licenseStatus.status === 'Expired' || licenseStatus.status === 'Invalid'
  const bannerColor = isExpired ? 'var(--gcp-red)' : 'var(--gcp-yellow)'

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

  return (
    <div style={{
      background: `color-mix(in srgb, ${bannerColor} 10%, var(--bg-secondary))`,
      borderBottom: `1px solid ${bannerColor}`,
      padding: '8px 20px',
      display: 'flex',
      alignItems: 'center',
      gap: 12,
      flexWrap: 'wrap',
      fontSize: 13,
      zIndex: 100,
    }}>
      <span style={{ color: bannerColor, fontWeight: 600 }}>
        {isExpired ? '⚠ License Expired' : `Trial — ${licenseStatus.trialDaysRemaining} day(s) remaining`}
      </span>
      <span style={{ color: 'var(--text-secondary)' }}>
        {isExpired
          ? 'API access restricted. Contact robram3000@gmail.com for a license key.'
          : `Contact robram3000@gmail.com to license this deployment.`}
      </span>

      <button
        onClick={() => { setShowForm(v => !v); setResult(null) }}
        style={{
          marginLeft: 'auto',
          padding: '4px 14px',
          background: 'transparent',
          border: `1px solid ${bannerColor}`,
          color: bannerColor,
          borderRadius: 4,
          cursor: 'pointer',
          fontSize: 12,
          fontWeight: 500,
        }}
      >
        {showForm ? 'Cancel' : 'Activate License'}
      </button>

      {showForm && (
        <form onSubmit={activate} style={{ width: '100%', display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap', marginTop: 4 }}>
          <input
            value={key}
            onChange={e => { setKey(e.target.value); setResult(null) }}
            placeholder="Paste license key from robram3000@gmail.com…"
            autoFocus
            style={{
              flex: 1,
              minWidth: 280,
              padding: '6px 10px',
              background: 'var(--bg-primary)',
              border: '1px solid var(--border-color)',
              borderRadius: 4,
              color: 'var(--text-primary)',
              fontSize: 12,
              fontFamily: 'monospace',
            }}
          />
          <button
            type="submit"
            disabled={activating || !key.trim()}
            style={{
              padding: '6px 16px',
              background: 'var(--gcp-blue)',
              border: 'none',
              borderRadius: 4,
              color: '#202124',
              fontWeight: 600,
              fontSize: 12,
              cursor: 'pointer',
              opacity: activating || !key.trim() ? 0.5 : 1,
            }}
          >
            {activating ? 'Activating…' : 'Activate'}
          </button>
          {result && (
            <span style={{ color: result.ok ? 'var(--gcp-green)' : 'var(--gcp-red)', fontSize: 12 }}>
              {result.msg}
            </span>
          )}
        </form>
      )}
    </div>
  )
}
