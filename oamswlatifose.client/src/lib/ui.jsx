// Small presentational helpers shared across the console: status colors, a
// dependency-free SVG sparkline (mirrors the GCP dashboard mini-charts), and icons.

export function statusColor(status) {
  switch ((status || '').toLowerCase()) {
    case 'present':
    case 'on time':
    case 'checked in':
      return 'var(--gcp-green)'
    case 'late':
    case 'late - excessive':
      return 'var(--gcp-yellow)'
    case 'absent':
      return 'var(--gcp-red)'
    case 'completed':
      return 'var(--gcp-blue)'
    default:
      return 'var(--text-muted)'
  }
}

export function statusBadge(status) {
  const c = statusColor(status)
  return (
    <span
      className="badge"
      style={{ background: `color-mix(in srgb, ${c} 16%, transparent)`, color: c }}
    >
      <span className="statusDot" style={{ background: c, margin: 0 }} />
      {status || '—'}
    </span>
  )
}

// Office / Outside / Unknown badge for the location column.
export function locationBadge(loc) {
  if (!loc) return <span className="muted">—</span>
  const office = /office/i.test(loc)
  const outside = /outside/i.test(loc)
  const c = office ? 'var(--gcp-green)' : outside ? 'var(--gcp-yellow)' : 'var(--text-muted)'
  const label = office ? 'Office' : outside ? 'Outside' : loc
  return (
    <span className="badge" style={{ background: `color-mix(in srgb, ${c} 16%, transparent)`, color: c }}>
      {label}
    </span>
  )
}

// Tiny area sparkline. `data` is an array of numbers; `color` a CSS color.
export function Sparkline({ data, color, height = 40 }) {
  const w = 120
  const h = height
  const vals = data && data.length ? data : [0, 0]
  const max = Math.max(1, ...vals)
  const step = vals.length > 1 ? w / (vals.length - 1) : w
  const pts = vals.map((v, i) => [i * step, h - (v / max) * (h - 6) - 3])
  const line = pts.map((p, i) => `${i === 0 ? 'M' : 'L'}${p[0].toFixed(1)},${p[1].toFixed(1)}`).join(' ')
  const area = `${line} L${w},${h} L0,${h} Z`
  return (
    <svg viewBox={`0 0 ${w} ${h}`} width="100%" height={h} preserveAspectRatio="none" aria-hidden="true">
      <path d={area} fill={color} opacity="0.16" />
      <path d={line} fill="none" stroke={color} strokeWidth="2" vectorEffect="non-scaling-stroke" />
    </svg>
  )
}

const ic = (path, props = {}) => (
  <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor"
       strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" {...props}>{path}</svg>
)

export const Icons = {
  monitor: ic(<><rect x="3" y="4" width="18" height="12" rx="1" /><path d="M8 20h8M12 16v4" /></>),
  clock: ic(<><circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" /></>),
  calendar: ic(<><rect x="3" y="4" width="18" height="18" rx="2" /><path d="M16 2v4M8 2v4M3 10h18" /></>),
  refresh: ic(<><path d="M21 12a9 9 0 1 1-2.64-6.36" /><path d="M21 3v6h-6" /></>),
  logout: ic(<><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" /><path d="M16 17l5-5-5-5M21 12H9" /></>),
  filter: ic(<polygon points="22 3 2 3 10 12.46 10 19 14 21 14 12.46 22 3" />),
  mail: ic(<><rect x="3" y="5" width="18" height="14" rx="2" /><path d="m3 7 9 6 9-6" /></>),
  check: ic(<path d="M20 6 9 17l-5-5" />),
  pin: ic(<><path d="M21 10c0 7-9 12-9 12s-9-5-9-12a9 9 0 0 1 18 0z" /><circle cx="12" cy="10" r="3" /></>),
  trash: ic(<><path d="M3 6h18M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2m2 0v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6" /></>),
  users: ic(<><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M23 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75" /></>),
  menu: ic(<><line x1="3" y1="6" x2="21" y2="6" /><line x1="3" y1="12" x2="21" y2="12" /><line x1="3" y1="18" x2="21" y2="18" /></>),
  close: ic(<><line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" /></>),
  eye: ic(<><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" /><circle cx="12" cy="12" r="3" /></>),
  eyeOff: ic(<><path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94" /><path d="M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19" /><line x1="1" y1="1" x2="23" y2="23" /></>),
  umbrella: ic(<><path d="M23 12a11 11 0 0 0-22 0"/><path d="M12 12v8a2 2 0 0 0 4 0"/></>),
}
