import { useMemo, useState } from 'react'
import { Icons } from '../../lib/ui'
import { exportCSV, exportExcel, printTable } from '../../lib/export'

// GCP-style table card: funnel filter bar + a sortable-looking header + rows.
// columns: [{ key, label, num?, render(row) }]. filterKeys: row keys to match the filter against.
// exportOptions: { filename: string, title?: string } — if provided, renders Print / CSV / Excel buttons.
export default function MonitoringTable({ columns, rows, loading, emptyText, filterKeys = [], exportOptions }) {
  const [filter, setFilter] = useState('')

  const visible = useMemo(() => {
    const f = filter.trim().toLowerCase()
    if (!f || !filterKeys.length) return rows
    return rows.filter((r) =>
      filterKeys.some((k) => String(r[k] ?? '').toLowerCase().includes(f)),
    )
  }, [rows, filter, filterKeys])

  return (
    <section className="tableCard">
      <div className="filterBar">
        <span className="filterIcon">{Icons.filter}</span>
        <input
          className="filterInput"
          placeholder="Filter"
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
          aria-label="Filter rows"
        />
        {exportOptions && (
          <div className="exportBtns">
            <button className="btnSm" onClick={() => printTable(visible, columns, exportOptions.title || exportOptions.filename)}>Print / PDF</button>
            <button className="btnSm" onClick={() => exportCSV(visible, columns, exportOptions.filename)}>CSV</button>
            <button className="btnSm" onClick={() => exportExcel(visible, columns, exportOptions.filename)}>Excel</button>
          </div>
        )}
      </div>
      <div className="tableScroll">
        <table className="table">
          <thead>
            <tr>
              {columns.map((c) => (
                <th key={c.key} className={`th${c.num ? ' thNum' : ''}${c.hideSm ? ' col-hide-sm' : ''}`}>{c.label}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {loading && visible.length === 0 ? (
              <tr><td className="stateCell" colSpan={columns.length}>Loading…</td></tr>
            ) : visible.length === 0 ? (
              <tr><td className="stateCell" colSpan={columns.length}>{emptyText || 'No records.'}</td></tr>
            ) : (
              visible.map((row, i) => (
                <tr key={row.id ?? i} className="row">
                  {columns.map((c) => (
                    <td key={c.key} className={`td${c.num ? ' tdNum' : ''}${c.hideSm ? ' col-hide-sm' : ''}`}>
                      {c.render ? c.render(row) : (row[c.key] ?? <span className="muted">—</span>)}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </section>
  )
}
