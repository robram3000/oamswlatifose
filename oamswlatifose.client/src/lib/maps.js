// Lazy loader for the Google Maps JavaScript API. The key comes from VITE_GOOGLE_MAPS_API_KEY
// (see .env). We pull in the `drawing` library (polygon drawing in the branch editor) and
// `geometry` (spherical helpers). The script is injected once and shared across all callers.

const KEY = import.meta.env.VITE_GOOGLE_MAPS_API_KEY || ''

let loaderPromise = null

/** True when an API key is configured — lets the UI degrade gracefully to coordinate inputs. */
export function isMapsConfigured() {
  return !!KEY
}

/** Resolves to the global `google` namespace, loading the Maps JS API on first call. */
export function loadGoogleMaps() {
  if (typeof window !== 'undefined' && window.google?.maps) return Promise.resolve(window.google)
  if (loaderPromise) return loaderPromise
  if (!KEY) return Promise.reject(new Error('Google Maps API key not configured (set VITE_GOOGLE_MAPS_API_KEY).'))

  loaderPromise = new Promise((resolve, reject) => {
    const callbackName = '__oamsGmapsInit'
    window[callbackName] = () => resolve(window.google)
    const script = document.createElement('script')
    script.src =
      'https://maps.googleapis.com/maps/api/js' +
      `?key=${encodeURIComponent(KEY)}` +
      '&libraries=drawing,geometry' +
      `&callback=${callbackName}&loading=async`
    script.async = true
    script.defer = true
    script.onerror = () => {
      loaderPromise = null
      reject(new Error('Failed to load Google Maps — check the API key and network.'))
    }
    document.head.appendChild(script)
  })
  return loaderPromise
}
