import { useEffect, useState } from 'react'
import { auth } from './lib/api'
import LoginPage from './features/auth/LoginPage'
import AttendanceConsole from './features/attendance/AttendanceConsole'

export default function App() {
  const [user, setUser] = useState(() => (auth.token ? auth.user : null))

  // Any API call that returns 401 dispatches this event (see lib/api.js).
  // We listen here so the console unmounts and the login screen appears immediately.
  useEffect(() => {
    const handle = () => setUser(null)
    window.addEventListener('auth:expired', handle)
    return () => window.removeEventListener('auth:expired', handle)
  }, [])

  if (!user) {
    return <LoginPage onLoggedIn={setUser} />
  }

  const signOut = () => {
    auth.clear()
    setUser(null)
  }

  return <AttendanceConsole user={user} onSignOut={signOut} />
}
