import { useEffect, useState } from 'react'
import { usersApi } from '../../lib/api'
import MonitoringTable from './MonitoringTable'

const EMPTY = {
  firstName: '', lastName: '', email: '', phone: '',
  position: '', department: '', username: '', password: '', roleId: '',
}

// Admin/HR panel: create an employee + linked login account, and list existing accounts.
export default function UserManager() {
  const [roles, setRoles] = useState([])
  const [users, setUsers] = useState([])
  const [form, setForm] = useState(EMPTY)
  const [saving, setSaving] = useState(false)
  const [loading, setLoading] = useState(true)
  const [notice, setNotice] = useState(null)

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
      // Default the dropdown to the basic "User" role when present.
      const u = list.find((r) => /user/i.test(r.name)) || list[list.length - 1]
      if (u) setForm((f) => ({ ...f, roleId: String(u.id) }))
    })
    loadUsers()
  }, [])

  const set = (k, v) => setForm((f) => ({ ...f, [k]: v }))

  const save = async () => {
    setNotice(null)
    if (!form.firstName.trim() || !form.lastName.trim()) { setNotice({ type: 'error', text: 'First and last name are required.' }); return }
    if (!form.email.trim()) { setNotice({ type: 'error', text: 'Email is required.' }); return }
    if (!form.username.trim()) { setNotice({ type: 'error', text: 'Username is required.' }); return }
    if (form.password.length < 6) { setNotice({ type: 'error', text: 'Password must be at least 6 characters.' }); return }
    if (!form.roleId) { setNotice({ type: 'error', text: 'Pick a role.' }); return }

    setSaving(true)
    const res = await usersApi.create({
      firstName: form.firstName.trim(),
      lastName: form.lastName.trim(),
      email: form.email.trim(),
      phone: form.phone.trim(),
      position: form.position.trim(),
      department: form.department.trim(),
      username: form.username.trim(),
      password: form.password,
      roleId: Number(form.roleId),
    })
    setSaving(false)
    if (res.isSuccess) {
      setNotice({ type: 'ok', text: `User “${res.data.username}” created.` })
      setForm((f) => ({ ...EMPTY, roleId: f.roleId }))
      await loadUsers()
    } else {
      setNotice({ type: 'error', text: res.message || 'Could not create user.' })
    }
  }

  return (
    <>
      <div className="panel">
        <h3 className="panel__title">Add user</h3>

        {notice && (
          <p className={`alert ${notice.type === 'ok' ? 'alert--ok' : 'alert--error'}`} style={{ marginBottom: 14 }}>
            {notice.text}
          </p>
        )}

        <div className="fieldRow">
          <div className="field"><label>First name</label>
            <input className="input" value={form.firstName} onChange={(e) => set('firstName', e.target.value)} /></div>
          <div className="field"><label>Last name</label>
            <input className="input" value={form.lastName} onChange={(e) => set('lastName', e.target.value)} /></div>
          <div className="field" style={{ minWidth: 220 }}><label>Email</label>
            <input className="input" type="email" value={form.email} onChange={(e) => set('email', e.target.value)} /></div>
          <div className="field"><label>Phone</label>
            <input className="input" value={form.phone} onChange={(e) => set('phone', e.target.value)} /></div>
        </div>

        <div className="fieldRow" style={{ marginTop: 12 }}>
          <div className="field"><label>Position</label>
            <input className="input" value={form.position} onChange={(e) => set('position', e.target.value)} /></div>
          <div className="field"><label>Department</label>
            <input className="input" value={form.department} onChange={(e) => set('department', e.target.value)} /></div>
          <div className="field"><label>Role</label>
            <select className="select" value={form.roleId} onChange={(e) => set('roleId', e.target.value)}>
              <option value="">Select role…</option>
              {roles.map((r) => <option key={r.id} value={r.id}>{r.name}</option>)}
            </select></div>
        </div>

        <div className="fieldRow" style={{ marginTop: 12 }}>
          <div className="field"><label>Username</label>
            <input className="input" autoComplete="off" value={form.username} onChange={(e) => set('username', e.target.value)} /></div>
          <div className="field"><label>Temporary password</label>
            <input className="input" type="text" autoComplete="new-password" value={form.password} onChange={(e) => set('password', e.target.value)} /></div>
        </div>

        <div className="actions">
          <button className="btnPrimary" onClick={save} disabled={saving}>{saving ? 'Creating…' : 'Create user'}</button>
        </div>
      </div>

      <MonitoringTable
        loading={loading}
        emptyText="No user accounts yet."
        filterKeys={['username', 'employeeName', 'roleName', 'department']}
        rows={users}
        columns={[
          { key: 'username', label: 'Username' },
          { key: 'employeeName', label: 'Name' },
          { key: 'email', label: 'Email' },
          { key: 'roleName', label: 'Role' },
          { key: 'department', label: 'Department' },
          { key: 'isActive', label: 'Active', render: (r) => (r.isActive ? 'Yes' : 'No') },
        ]}
      />
    </>
  )
}
