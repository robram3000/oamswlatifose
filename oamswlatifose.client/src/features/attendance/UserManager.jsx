import { useEffect, useState } from 'react'
import { usersApi } from '../../lib/api'
import MonitoringTable from './MonitoringTable'
import { Icons } from '../../lib/ui'

const EMPTY = {
  firstName: '', lastName: '', email: '', phone: '',
  position: '', department: '', username: '', password: '', confirmPw: '', roleId: '',
}

function activeBadge(isActive) {
  const c = isActive ? 'var(--gcp-green)' : 'var(--gcp-red)'
  return (
    <span className="badge" style={{ background: `color-mix(in srgb, ${c} 16%, transparent)`, color: c }}>
      <span className="statusDot" style={{ background: c, margin: 0 }} />
      {isActive ? 'Active' : 'Inactive'}
    </span>
  )
}

export default function UserManager() {
  const [roles, setRoles] = useState([])
  const [users, setUsers] = useState([])
  const [loading, setLoading] = useState(true)

  // Create modal
  const [showCreate, setShowCreate] = useState(false)
  const [form, setForm] = useState(EMPTY)
  const [saving, setSaving] = useState(false)
  const [createNotice, setCreateNotice] = useState(null)

  // Edit modal
  const [editUser, setEditUser] = useState(null)
  const [editForm, setEditForm] = useState({})
  const [editSaving, setEditSaving] = useState(false)
  const [editNotice, setEditNotice] = useState(null)

  const loadUsers = async () => {
    setLoading(true)
    const res = await usersApi.list()
    setUsers(res.isSuccess && Array.isArray(res.data) ? res.data : [])
    setLoading(false)
  }

  useEffect(() => {
    usersApi.roles().then((res) => {
      const list = res.isSuccess && Array.isArray(res.data) ? res.data : []
      setRoles(list)
      const u = list.find((r) => /user/i.test(r.name)) || list[list.length - 1]
      if (u) setForm((f) => ({ ...f, roleId: String(u.id) }))
    })
    loadUsers()
  }, [])

  const set = (k, v) => setForm((f) => ({ ...f, [k]: v }))
  const setE = (k, v) => setEditForm((f) => ({ ...f, [k]: v }))

  const openCreate = () => { setCreateNotice(null); setShowCreate(true) }
  const closeCreate = () => { setShowCreate(false); setCreateNotice(null); setForm((f) => ({ ...EMPTY, roleId: f.roleId })) }

  const save = async () => {
    setCreateNotice(null)
    if (!form.firstName.trim() || !form.lastName.trim()) { setCreateNotice({ type: 'error', text: 'First and last name are required.' }); return }
    if (!form.email.trim()) { setCreateNotice({ type: 'error', text: 'Email is required.' }); return }
    if (!form.username.trim()) { setCreateNotice({ type: 'error', text: 'Username is required.' }); return }
    if (form.password.length < 6) { setCreateNotice({ type: 'error', text: 'Password must be at least 6 characters.' }); return }
    if (form.password !== form.confirmPw) { setCreateNotice({ type: 'error', text: 'Passwords do not match.' }); return }
    if (!form.roleId) { setCreateNotice({ type: 'error', text: 'Pick a role.' }); return }

    setSaving(true)
    const res = await usersApi.create({
      firstName: form.firstName.trim(), lastName: form.lastName.trim(),
      email: form.email.trim(), phone: form.phone.trim(),
      position: form.position.trim(), department: form.department.trim(),
      username: form.username.trim(), password: form.password,
      roleId: Number(form.roleId),
    })
    setSaving(false)
    if (res.isSuccess) {
      closeCreate()
      await loadUsers()
    } else {
      setCreateNotice({ type: 'error', text: res.message || 'Could not create user.' })
    }
  }

  const openEdit = (row) => {
    setEditUser(row)
    setEditNotice(null)
    setEditForm({
      firstName: (row.employeeName || '').split(' ')[0] || '',
      lastName: (row.employeeName || '').split(' ').slice(1).join(' ') || '',
      email: row.email || '',
      phone: '', position: '',
      department: row.department || '',
      roleId: String(row.roleId || ''),
      isActive: row.isActive !== false,
      newPassword: '', confirmNewPw: '',
    })
  }

  const saveEdit = async () => {
    setEditNotice(null)
    if (!editForm.firstName?.trim() || !editForm.lastName?.trim()) { setEditNotice({ type: 'error', text: 'First and last name are required.' }); return }
    if (!editForm.email?.trim()) { setEditNotice({ type: 'error', text: 'Email is required.' }); return }
    if (!editForm.roleId) { setEditNotice({ type: 'error', text: 'Pick a role.' }); return }
    if (editForm.newPassword && editForm.newPassword.length < 6) { setEditNotice({ type: 'error', text: 'New password must be at least 6 characters.' }); return }
    if (editForm.newPassword && editForm.newPassword !== editForm.confirmNewPw) { setEditNotice({ type: 'error', text: 'Passwords do not match.' }); return }

    setEditSaving(true)
    const res = await usersApi.update(editUser.id, {
      firstName: editForm.firstName.trim(), lastName: editForm.lastName.trim(),
      email: editForm.email.trim(), phone: editForm.phone?.trim() || '',
      position: editForm.position?.trim() || '', department: editForm.department?.trim() || '',
      roleId: Number(editForm.roleId), isActive: editForm.isActive,
      newPassword: editForm.newPassword || null,
    })
    setEditSaving(false)
    if (res.isSuccess) { setEditUser(null); await loadUsers() }
    else setEditNotice({ type: 'error', text: res.message || 'Could not update user.' })
  }

  return (
    <>
      {/* ── Header ─────────────────────────────────────────── */}
      <div className="topRow" style={{ marginBottom: 8 }}>
        <div>
          <h1 className="pageTitle" style={{ fontSize: 18 }}>All users</h1>
          <p className="pageSub">Employee accounts and their roles.</p>
        </div>
        <button className="btnPrimary" onClick={openCreate}>+ Add user</button>
      </div>

      {/* ── Users table ─────────────────────────────────────── */}
      <MonitoringTable
        loading={loading}
        emptyText="No user accounts yet."
        filterKeys={['username', 'employeeName', 'roleName', 'department']}
        rows={users}
        columns={[
          { key: 'username', label: 'Username' },
          { key: 'employeeName', label: 'Name' },
          { key: 'email', label: 'Email', hideSm: true },
          { key: 'roleName', label: 'Role' },
          { key: 'department', label: 'Dept', hideSm: true },
          { key: 'hiredAtFormatted', label: 'Hired', hideSm: true, render: (r) => r.hiredAtFormatted || <span className="muted">—</span> },
          { key: 'isActive', label: 'Status', render: (r) => activeBadge(r.isActive) },
          { key: '_actions', label: '', render: (r) => (
            <button className="btnSm" onClick={() => openEdit(r)}>Edit</button>
          ) },
        ]}
      />

      {/* ── Create modal ─────────────────────────────────────── */}
      {showCreate && (
        <div className="modalOverlay" onClick={closeCreate}>
          <div className="modal modal--wide" onClick={(e) => e.stopPropagation()}>
            <div className="modal__header">
              <h3 className="modal__title" style={{ margin: 0 }}>New user</h3>
              <button className="iconBtn" onClick={closeCreate}>{Icons.close}</button>
            </div>
            <div style={{ padding: '0 24px 24px' }}>
              {createNotice && (
                <p className={`alert ${createNotice.type === 'ok' ? 'alert--ok' : 'alert--error'}`} style={{ margin: '14px 0' }}>
                  {createNotice.text}
                </p>
              )}
              <div className="fieldRow" style={{ marginTop: 16 }}>
                <div className="field"><label>First name *</label>
                  <input className="input" value={form.firstName} onChange={(e) => set('firstName', e.target.value)} /></div>
                <div className="field"><label>Last name *</label>
                  <input className="input" value={form.lastName} onChange={(e) => set('lastName', e.target.value)} /></div>
                <div className="field" style={{ minWidth: 200 }}><label>Email *</label>
                  <input className="input" type="email" value={form.email} onChange={(e) => set('email', e.target.value)} /></div>
                <div className="field"><label>Phone</label>
                  <input className="input" value={form.phone} onChange={(e) => set('phone', e.target.value)} /></div>
              </div>
              <div className="fieldRow" style={{ marginTop: 12 }}>
                <div className="field"><label>Position</label>
                  <input className="input" value={form.position} onChange={(e) => set('position', e.target.value)} /></div>
                <div className="field"><label>Department</label>
                  <input className="input" value={form.department} onChange={(e) => set('department', e.target.value)} /></div>
                <div className="field"><label>Role *</label>
                  <select className="select" value={form.roleId} onChange={(e) => set('roleId', e.target.value)}>
                    <option value="">Select role...</option>
                    {roles.map((r) => <option key={r.id} value={r.id}>{r.name}</option>)}
                  </select></div>
              </div>
              <div className="fieldRow" style={{ marginTop: 12 }}>
                <div className="field"><label>Username *</label>
                  <input className="input" autoComplete="off" value={form.username} onChange={(e) => set('username', e.target.value)} /></div>
                <div className="field"><label>Password *</label>
                  <input className="input" type="password" autoComplete="new-password" value={form.password} onChange={(e) => set('password', e.target.value)} /></div>
                <div className="field"><label>Confirm password *</label>
                  <input className="input" type="password" autoComplete="new-password" value={form.confirmPw} onChange={(e) => set('confirmPw', e.target.value)} /></div>
              </div>
              <div className="modal__actions">
                <button className="btnGhost" onClick={closeCreate}>Cancel</button>
                <button className="btnPrimary" onClick={save} disabled={saving}>{saving ? 'Creating...' : 'Create user'}</button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ── Edit modal ──────────────────────────────────────── */}
      {editUser && (
        <div className="modalOverlay" onClick={() => setEditUser(null)}>
          <div className="modal modal--wide" onClick={(e) => e.stopPropagation()}>
            <div className="modal__header">
              <h3 className="modal__title" style={{ margin: 0 }}>Edit user · {editUser.username}{editUser.hiredAtFormatted ? ` · Hired ${editUser.hiredAtFormatted}` : ''}</h3>
              <button className="iconBtn" onClick={() => setEditUser(null)}>{Icons.close}</button>
            </div>
            <div style={{ padding: '0 24px 24px' }}>
              {editNotice && (
                <p className={`alert ${editNotice.type === 'ok' ? 'alert--ok' : 'alert--error'}`} style={{ margin: '14px 0' }}>
                  {editNotice.text}
                </p>
              )}
              <div className="fieldRow" style={{ marginTop: 16 }}>
                <div className="field"><label>First name *</label>
                  <input className="input" value={editForm.firstName || ''} onChange={(e) => setE('firstName', e.target.value)} /></div>
                <div className="field"><label>Last name *</label>
                  <input className="input" value={editForm.lastName || ''} onChange={(e) => setE('lastName', e.target.value)} /></div>
                <div className="field" style={{ minWidth: 200 }}><label>Email *</label>
                  <input className="input" type="email" value={editForm.email || ''} onChange={(e) => setE('email', e.target.value)} /></div>
              </div>
              <div className="fieldRow" style={{ marginTop: 12 }}>
                <div className="field"><label>Department</label>
                  <input className="input" value={editForm.department || ''} onChange={(e) => setE('department', e.target.value)} /></div>
                <div className="field"><label>Role *</label>
                  <select className="select" value={editForm.roleId || ''} onChange={(e) => setE('roleId', e.target.value)}>
                    <option value="">Select role...</option>
                    {roles.map((r) => <option key={r.id} value={r.id}>{r.name}</option>)}
                  </select></div>
                <div className="field">
                  <label>Status</label>
                  <label className="toggleLabel">
                    <input type="checkbox" checked={editForm.isActive || false} onChange={(e) => setE('isActive', e.target.checked)} />
                    {editForm.isActive ? ' Active' : ' Inactive'}
                  </label>
                </div>
              </div>
              <div className="fieldRow" style={{ marginTop: 12 }}>
                <div className="field"><label>New password <span className="muted">(leave blank to keep)</span></label>
                  <input className="input" type="password" autoComplete="new-password" value={editForm.newPassword || ''} onChange={(e) => setE('newPassword', e.target.value)} /></div>
                <div className="field"><label>Confirm new password</label>
                  <input className="input" type="password" autoComplete="new-password" value={editForm.confirmNewPw || ''} onChange={(e) => setE('confirmNewPw', e.target.value)} /></div>
              </div>
              <div className="modal__actions">
                <button className="btnGhost" onClick={() => setEditUser(null)}>Cancel</button>
                <button className="btnPrimary" onClick={saveEdit} disabled={editSaving}>{editSaving ? 'Saving...' : 'Save changes'}</button>
              </div>
            </div>
          </div>
        </div>
      )}
    </>
  )
}
