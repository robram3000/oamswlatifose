import { useEffect, useState } from 'react'
import { auth, licenseApi } from './lib/api'
import LoginPage from './features/auth/LoginPage'
import AttendanceConsole from './features/attendance/AttendanceConsole'
import LicenseBanner from './features/license/LicenseBanner'
import LicenseGate from './features/license/LicenseGate'

export default function App() {
  const [user, setUser] = useState(() => (auth.token ? auth.user : null))
  const [licenseStatus, setLicenseStatus] = useState(null)

  useEffect(() => {
    const handle = () => setUser(null)
    window.addEventListener('auth:expired', handle)
    return () => window.removeEventListener('auth:expired', handle)
  }, [])

  useEffect(() => {
    licenseApi.status().then(res => {
      if (res.isSuccess) setLicenseStatus(res.data)
    })
  }, [])

  const isExpired = licenseStatus?.status === 'Expired' || licenseStatus?.status === 'Invalid'

  if (!user) {
    return (
      <div style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
        {licenseStatus && <LicenseBanner licenseStatus={licenseStatus} onActivated={setLicenseStatus} />}
        <div style={{ flex: 1 }}>
          <LoginPage onLoggedIn={setUser} />
        </div>
      </div>
    )
  }

  const signOut = () => {
    auth.clear()
    setUser(null)
  }

  if (isExpired) {
    return <LicenseGate licenseStatus={licenseStatus} onActivated={setLicenseStatus} onSignOut={signOut} />
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      {licenseStatus && <LicenseBanner licenseStatus={licenseStatus} onActivated={setLicenseStatus} />}
      <div style={{ flex: 1, overflow: 'hidden' }}>
        <AttendanceConsole user={user} onSignOut={signOut} />
      </div>
    </div>
  )
}
