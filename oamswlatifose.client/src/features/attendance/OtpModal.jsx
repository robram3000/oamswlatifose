import { useEffect, useRef, useState } from 'react'
import { Icons } from '../../lib/ui'

// Modal shown after "Time In": the employee enters the code emailed to them.
// onVerify(code) and onResend() return the API ServiceResponse ({ isSuccess, message }).
export default function OtpModal({ info, onVerify, onResend, onClose }) {
  const [code, setCode] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [resending, setResending] = useState(false)
  const [secsLeft, setSecsLeft] = useState((info?.expiresInMinutes || 10) * 60)
  const inputRef = useRef(null)

  useEffect(() => { inputRef.current?.focus() }, [])

  useEffect(() => {
    if (secsLeft <= 0) return
    const id = setInterval(() => setSecsLeft((s) => Math.max(0, s - 1)), 1000)
    return () => clearInterval(id)
  }, [secsLeft])

  const mmss = `${String(Math.floor(secsLeft / 60)).padStart(2, '0')}:${String(secsLeft % 60).padStart(2, '0')}`

  const verify = async (e) => {
    e?.preventDefault()
    if (code.length < 4) return
    setBusy(true)
    setError('')
    const res = await onVerify(code)
    setBusy(false)
    if (!res.isSuccess) {
      setError(res.message || 'Verification failed')
      setCode('')
      inputRef.current?.focus()
    }
    // On success the parent closes the modal.
  }

  const resend = async () => {
    setResending(true)
    setError('')
    const res = await onResend()
    setResending(false)
    if (res.isSuccess) {
      setCode('')
      setSecsLeft((res.data?.expiresInMinutes || 10) * 60)
      // Show delivery error from resend if SMTP failed
      if (res.data?.sent === false) {
        setError(res.data?.message || 'Email could not be delivered. Check SMTP settings.')
      } else {
        inputRef.current?.focus()
      }
    } else {
      setError(res.message || 'Could not resend the code')
    }
  }

  // Detect email delivery failure from the initial send
  const emailFailed = info?.sent === false

  return (
    <div className="overlay" role="dialog" aria-modal="true" onMouseDown={(e) => e.target === e.currentTarget && onClose()}>
      <form className="modal" onSubmit={verify}>
        <h2 className="modal__title">Verify your clock-in</h2>
        <p className="modal__sub">
          <span style={{ display: 'inline-flex', verticalAlign: 'middle', marginRight: 6 }}>{Icons.mail}</span>
          {emailFailed
            ? <>Email delivery failed for <strong>{info?.emailMasked || 'your email'}</strong>.</>
            : <>We emailed a code to <strong>{info?.emailMasked || 'your email'}</strong>.</>}
          {info?.requestedTimeFormatted && <> Time-in captured at <strong>{info.requestedTimeFormatted}</strong>.</>}
        </p>

        {emailFailed && (
          <p className="alert alert--error" style={{ marginBottom: 12 }}>
            {info?.message || 'Could not send the verification email. Try resending or contact Admin.'}
          </p>
        )}

        {info?.workLocation && (
          <p className={`alert ${info.onSite ? 'alert--ok' : info.workLocation === 'Unknown' ? 'alert--info' : 'alert--info'}`}
             style={{ marginBottom: 16, display: 'flex', alignItems: 'center', gap: 8 }}>
            {Icons.pin}
            {info.onSite
              ? <>At <strong>{info.branchName || 'office'}</strong> · recorded as <strong>Office</strong></>
              : info.workLocation === 'Unknown'
                ? <>Location unavailable · recorded as <strong>Unknown</strong></>
                : <>Off-site{info.distanceMeters != null ? ` (~${info.distanceMeters} m from nearest branch)` : ''} · recorded as <strong>Outside</strong></>}
          </p>
        )}

        <input
          ref={inputRef}
          className="otpInput"
          inputMode="numeric"
          autoComplete="one-time-code"
          maxLength={8}
          placeholder="••••••"
          value={code}
          onChange={(e) => setCode(e.target.value.replace(/\D/g, ''))}
          aria-label="Verification code"
        />

        {error && <p className="alert alert--error" style={{ marginTop: 14 }}>{error}</p>}

        <p className="modal__sub" style={{ margin: '14px 0 0', textAlign: 'center' }}>
          {secsLeft > 0 ? <>Code expires in <strong>{mmss}</strong></> : <span style={{ color: 'var(--gcp-red)' }}>Code expired</span>}
        </p>

        <div className="modal__actions">
          <button type="button" className="linkBtn" onClick={resend} disabled={resending}>
            {resending ? 'Resending…' : 'Resend code'}
          </button>
          <div style={{ display: 'flex', gap: 10 }}>
            <button type="button" className="btnGhost" onClick={onClose} disabled={busy}>Cancel</button>
            <button type="submit" className="btnPrimary" disabled={busy || code.length < 4}>
              {busy ? <span className="spinner" /> : Icons.check}
              {busy ? 'Verifying…' : 'Verify'}
            </button>
          </div>
        </div>
      </form>
    </div>
  )
}
