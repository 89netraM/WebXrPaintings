/// <reference types="vite/client" />

interface XRSessionInit {
  trackedImages: any;
}

interface XRFrame {
  getImageTrackingResults(): any;
}
