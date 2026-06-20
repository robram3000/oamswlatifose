import { useState } from 'react'
import { auth } from './lib/api'
import LoginPage from './features/auth/LoginPage'
import AttendanceConsole from './features/attendance/AttendanceConsole'

export default function App() {
  // Treat a stored token as "signed in"; a 401 from any call clears it (see lib/api).
  const [user, setUser] = useState(() => (auth.token ? auth.user : null))

  if (!user) {
    return <LoginPage onLoggedIn={setUser} />
  }

  const signOut = () => {
    auth.clear()
    setUser(null)
  }

  return <AttendanceConsole user={user} onSignOut={signOut} />
}
