/// <reference types="vite/client" />

declare module "three/examples/jsm/webxr/ARButton" {
  export class ARButton {
    static createButton(renderer: WebGLRenderer, options?: XRSessionInit);
  }
}

interface XRSessionInit {
  trackedImages: any;
}

interface XRFrame {
  getImageTrackingResults(): any;
}
