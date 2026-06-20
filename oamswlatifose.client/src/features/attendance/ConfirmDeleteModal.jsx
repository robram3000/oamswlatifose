import { useEffect, useRef, useState } from 'react'
import { Icons } from '../../lib/ui'

/**
 * Confirmation modal that requires the user to type `confirmText` before
 * the Delete button becomes active. Pass `confirmText` as the item name or
 * any phrase — it is shown in a <code> block so the user knows exactly what
 * to type.
 *
 * Props:
 *   title       – modal heading (e.g. "Delete schedule")
 *   description – sentence describing what will be deleted
 *   confirmText – string the user must type exactly (case-sensitive)
 *   onConfirm   – async fn called when user clicks Delete
 *   onClose     – fn called when user cancels or clicks outside
 */
export default function ConfirmDeleteModal({ title, description, confirmText, onConfirm, onClose }) {
  const [typed, setTyped] = useState('')
  const [busy, setBusy] = useState(false)
  const inputRef = useRef(null)

  useEffect(() => { inputRef.current?.focus() }, [])

  const matches = typed === confirmText

  const handleConfirm = async () => {
    if (!matches || busy) return
    setBusy(true)
    await onConfirm()
    setBusy(false)
  }

  const handleKey = (e) => {
    if (e.key === 'Enter') handleConfirm()
    if (e.key === 'Escape') onClose()
  }

  return (
    <div className="modalOverlay" onClick={onClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 440 }}>
        <div style={{ display: 'flex', alignItems: 'flex-start', gap: 14, marginBottom: 16 }}>
          <span style={{
            flexShrink: 0, width: 36, height: 36, borderRadius: '50%',
            background: 'rgba(242,139,130,.15)', display: 'flex', alignItems: 'center', justifyContent: 'center',
            color: 'var(--gcp-red)',
          }}>
            {Icons.trash}
          </span>
          <div>
            <h3 className="modal__title" style={{ margin: '0 0 4px' }}>{title}</h3>
            <p style={{ margin: 0, fontSize: 13, color: 'var(--text-secondary)', lineHeight: 1.5 }}>{description}</p>
          </div>
        </div>

        <p style={{ fontSize: 13, color: 'var(--text-secondary)', margin: '0 0 8px' }}>
          Type <code style={{
            background: 'var(--bg-secondary)', border: '1px solid var(--border-color)',
            borderRadius: 4, padding: '1px 6px', fontFamily: 'monospace', color: 'var(--text-primary)',
            userSelect: 'all',
          }}>{confirmText}</code> to confirm:
        </p>

        <input
          ref={inputRef}
          className={`input${typed && !matches ? ' input--error' : ''}`}
          style={{ width: '100%', boxSizing: 'border-box', fontFamily: 'monospace' }}
          value={typed}
          onChange={(e) => setTyped(e.target.value)}
          onKeyDown={handleKey}
          placeholder={confirmText}
          autoComplete="off"
          spellCheck={false}
        />

        {typed && !matches && (
          <p style={{ fontSize: 12, color: 'var(--gcp-red)', margin: '6px 0 0' }}>
            Text does not match — check capitalisation.
          </p>
        )}

        <div className="modal__actions" style={{ marginTop: 20 }}>
          <button className="btnGhost" onClick={onClose} disabled={busy}>Cancel</button>
          <button
            onClick={handleConfirm}
            disabled={!matches || busy}
            style={{
              height: 36, padding: '0 18px', borderRadius: 6, border: 'none',
              background: matches ? 'var(--gcp-red)' : 'color-mix(in srgb, var(--gcp-red) 30%, transparent)',
              color: matches ? '#fff' : 'rgba(255,255,255,.4)',
              fontWeight: 600, fontSize: 13, cursor: matches ? 'pointer' : 'not-allowed',
              transition: 'background .15s, color .15s',
              display: 'flex', alignItems: 'center', gap: 6,
            }}
          >
            {busy ? <span className="spinner" style={{ borderTopColor: '#fff' }} /> : Icons.trash}
            {busy ? 'Deleting…' : 'Delete'}
          </button>
        </div>
      </div>
    </div>
  )
}
