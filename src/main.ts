import "./styles.css";
import { Mesh, MeshBasicMaterial, PerspectiveCamera, PlaneGeometry, Scene, SRGBColorSpace, TextureLoader, WebGLRenderer } from "three";
import { ARButton } from "three/examples/jsm/webxr/ARButton";

window.addEventListener("load", async () => {
  const canvas = document.querySelector("canvas") as HTMLCanvasElement;
  const renderer = new WebGLRenderer({ canvas, antialias: true, alpha: true });
  renderer.setSize(window.innerWidth, window.innerHeight);
  renderer.setPixelRatio(window.devicePixelRatio);
  renderer.xr.enabled = true;

  const camera = new PerspectiveCamera(70, window.innerWidth / window.innerHeight, 0.01, 40);

  const targetPainting = await window.createImageBitmap(await (await window.fetch("target.png")).blob());
  const texture = await new TextureLoader().loadAsync("replacement.png");
  texture.colorSpace = SRGBColorSpace;
  const scale = 2;

  const scene = new Scene();
  const targetAspectRatio = targetPainting.width / targetPainting.height;
  const textureAspectRatio = texture.image.width / texture.image.height;
  console.log({ targetAspectRatio, textureAspectRatio });
  const [width, height] = targetAspectRatio < textureAspectRatio
    ? [1, 1 / textureAspectRatio]
    : [textureAspectRatio / targetAspectRatio, 1 / targetAspectRatio];
  const geometry = new PlaneGeometry(width * scale, height * scale);
  geometry.rotateX(-Math.PI / 2);
  const replacementPainting = new Mesh(geometry, new MeshBasicMaterial({ map: texture, transparent: true }));
  replacementPainting.matrixAutoUpdate = false;
  replacementPainting.visible = false;
  scene.add(replacementPainting);

  const arButton = ARButton.createButton(renderer, {
    requiredFeatures: ["image-tracking"],
    trackedImages: [
      {
        image: targetPainting,
        widthInMeters: 1,
      },
    ],
  });
  document.body.appendChild(arButton);

  renderer.setAnimationLoop((_, frame) => {
    replacementPainting.visible = false;
    if (frame) {
      for (const imageTracker of frame.getImageTrackingResults()) {
        const pose = frame.getPose(imageTracker.imageSpace, renderer.xr.getReferenceSpace()!)!;

        if (imageTracker.trackingState == "tracked") {
          replacementPainting.visible = true;
          replacementPainting.matrix.fromArray(pose.transform.matrix);
          break;
        }
      }
    }

    renderer.render(scene, camera);
  });
});
