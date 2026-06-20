// Client-side export utilities: CSV, Excel (SpreadsheetML), Print, and CSV import parser.

function plainText(row, col) {
  const v = row[col.key]
  if (v == null) return ''
  if (typeof v === 'object') return '' // React element — skip
  return String(v)
}

function csvEsc(val) {
  const s = String(val ?? '')
  return s.includes(',') || s.includes('"') || s.includes('\n')
    ? '"' + s.replace(/"/g, '""') + '"'
    : s
}

function xmlEsc(s) {
  return String(s ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
}

function htmlEsc(s) {
  return String(s ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
}

const exportCols = (columns) => columns.filter((c) => !c.key.startsWith('_'))

function downloadBlob(blob, filename) {
  const url = URL.createObjectURL(blob)
  const a = Object.assign(document.createElement('a'), { href: url, download: filename })
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  URL.revokeObjectURL(url)
}

export function exportCSV(rows, columns, filename = 'export') {
  const cols = exportCols(columns)
  const lines = [
    cols.map((c) => csvEsc(c.label)).join(','),
    ...rows.map((r) => cols.map((c) => csvEsc(plainText(r, c))).join(',')),
  ]
  downloadBlob(
    new Blob([lines.join('\r\n')], { type: 'text/csv;charset=utf-8;' }),
    `${filename}.csv`,
  )
}

export function exportExcel(rows, columns, filename = 'export') {
  const cols = exportCols(columns)
  const cell = (v) => `<Cell><Data ss:Type="String">${xmlEsc(v)}</Data></Cell>`
  const xmlRow = (cells) => `<Row>${cells}</Row>`
  const header = xmlRow(cols.map((c) => cell(c.label)).join(''))
  const body = rows
    .map((r) => xmlRow(cols.map((c) => cell(plainText(r, c))).join('')))
    .join('\n')
  const xml = `<?xml version="1.0"?>
<Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet"
 xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet">
<Worksheet ss:Name="Attendance"><Table>${header}${body}</Table></Worksheet>
</Workbook>`
  downloadBlob(
    new Blob([xml], { type: 'application/vnd.ms-excel;charset=utf-8;' }),
    `${filename}.xls`,
  )
}

export function printTable(rows, columns, title = '') {
  const cols = exportCols(columns)
  const thRow = cols.map((c) => `<th>${htmlEsc(c.label)}</th>`).join('')
  const tdRows = rows
    .map((r) => `<tr>${cols.map((c) => `<td>${htmlEsc(plainText(r, c))}</td>`).join('')}</tr>`)
    .join('')
  const html = `<!DOCTYPE html><html><head>
<meta charset="utf-8">
<title>${htmlEsc(title || 'Attendance Report')}</title>
<style>
body{font-family:Arial,sans-serif;font-size:11px;margin:20px}
h2{font-size:14px;margin-bottom:10px}
table{border-collapse:collapse;width:100%}
th,td{border:1px solid #bbb;padding:5px 8px;text-align:left}
th{background:#f2f2f2;font-weight:600}
tr:nth-child(even){background:#fafafa}
@media print{button{display:none}}
</style>
</head><body>
${title ? `<h2>${htmlEsc(title)}</h2>` : ''}
<table><thead><tr>${thRow}</tr></thead><tbody>${tdRows}</tbody></table>
<script>window.onload=()=>{window.print()}</script>
</body></html>`
  const w = window.open('', '_blank', 'width=950,height=700')
  if (!w) return
  w.document.write(html)
  w.document.close()
}

// ── CSV import ─────────────────────────────────────────────────────────

function parseCsvRow(line) {
  const cells = []
  let inq = false, cell = ''
  for (let i = 0; i < line.length; i++) {
    const c = line[i]
    if (c === '"') {
      if (inq && line[i + 1] === '"') { cell += '"'; i++ }
      else inq = !inq
    } else if (c === ',' && !inq) {
      cells.push(cell.trim()); cell = ''
    } else {
      cell += c
    }
  }
  cells.push(cell.trim())
  return cells
}

function normalizeTime(val) {
  const s = (val ?? '').trim()
  if (!s) return null
  if (/^\d{2}:\d{2}:\d{2}$/.test(s)) return s
  if (/^\d{1,2}:\d{2}$/.test(s)) return s.padStart(5, '0') + ':00'
  return null
}

// Returns array of CreateAttendanceDTO-shaped objects ready to POST to /api/attendance/admin/bulk-import
export function parseCsvImport(text) {
  const lines = text.split(/\r?\n/).map((l) => l.trim()).filter(Boolean)
  if (lines.length < 2) return []
  const headers = parseCsvRow(lines[0]).map((h) => h.toLowerCase().replace(/\s+/g, ''))
  const idx = (name) => headers.indexOf(name)
  const records = []
  for (let i = 1; i < lines.length; i++) {
    const cells = parseCsvRow(lines[i])
    const empId = parseInt(cells[idx('employeeid')] ?? '', 10)
    const date = (cells[idx('date')] ?? cells[idx('attendancedate')] ?? '').trim()
    if (!empId || !date) continue
    records.push({
      employeeId: empId,
      attendanceDate: date,
      timeIn: normalizeTime(cells[idx('timein')]),
      timeOut: normalizeTime(cells[idx('timeout')]),
      status: (cells[idx('status')] ?? '').trim() || null,
      shift: (cells[idx('shift')] ?? '').trim() || null,
      remarks: (cells[idx('remarks')] ?? '').trim() || null,
    })
  }
  return records
}

const TEMPLATE = 'EmployeeId,Date,TimeIn,TimeOut,Status,Shift,Remarks\r\n1,2024-01-15,08:30,17:00,Present,Morning,\r\n'

export function downloadImportTemplate() {
  downloadBlob(
    new Blob([TEMPLATE], { type: 'text/csv;charset=utf-8;' }),
    'attendance_import_template.csv',
  )
}
