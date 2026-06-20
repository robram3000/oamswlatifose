// Browser geolocation, wrapped to never reject — resolves to null when permission is
// denied, the API is unavailable, or it times out, so the caller can fall back to an
// "Outside / Unknown" clock-in instead of breaking the flow.
export function getCurrentLocation(timeoutMs = 8000) {
  return new Promise((resolve) => {
    if (typeof navigator === 'undefined' || !('geolocation' in navigator)) {
      resolve(null)
      return
    }
    navigator.geolocation.getCurrentPosition(
      (pos) => resolve({ latitude: pos.coords.latitude, longitude: pos.coords.longitude }),
      () => resolve(null),
      { enableHighAccuracy: true, timeout: timeoutMs, maximumAge: 60000 },
    )
  })
}
