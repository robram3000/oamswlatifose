import { useState } from 'react'
import { authApi, auth } from '../../lib/api'

export default function LoginPage({ onLoggedIn }) {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const submit = async (e) => {
    e.preventDefault()
    setError('')
    setLoading(true)
    const res = await authApi.login(username.trim(), password)
    setLoading(false)

    if (!res.isSuccess) {
      setError(res.message || 'Sign in failed')
      return
    }
    auth.set(res.data.accessToken, res.data.user)
    onLoggedIn(res.data.user)
  }

  return (
    <div className="loginWrap">
      <form className="loginCard" onSubmit={submit}>
        <img src="/logo.svg" alt="AGLIPAY" className="loginLogo" />
        <h1 className="brandTitle brandTitle--login">AGLIPAY</h1>
        <p className="sub">Attendance monitoring</p>

        {error && <p className="alert alert--error" style={{ marginBottom: 16 }}>{error}</p>}

        <div className="loginField">
          <label htmlFor="u">Username</label>
          <input id="u" value={username} onChange={(e) => setUsername(e.target.value)}
                 autoComplete="username" autoFocus required />
        </div>
        <div className="loginField">
          <label htmlFor="p">Password</label>
          <input id="p" type="password" value={password} onChange={(e) => setPassword(e.target.value)}
                 autoComplete="current-password" required />
        </div>

        <button className="loginBtn" type="submit" disabled={loading || !username || !password}>
          {loading ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
    </div>
  )
}
