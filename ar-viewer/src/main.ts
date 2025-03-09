import "./styles.css";
import { Camera, Mesh, MeshBasicMaterial, PlaneGeometry, Scene, SRGBColorSpace, TextureLoader, WebGLRenderer } from "three";

window.addEventListener("load", async () => {
  const isArSupported = await navigator.xr?.isSessionSupported("immersive-ar");
  if (!isArSupported) {
    return;
  }

  const renderer = new WebGLRenderer({ antialias: true, alpha: true });
  renderer.setSize(window.innerWidth, window.innerHeight);
  renderer.setPixelRatio(window.devicePixelRatio);
  renderer.xr.enabled = true;

  const camera = new Camera();

  const targetPainting = await window.createImageBitmap(await (await window.fetch("target")).blob());
  const texture = await new TextureLoader().loadAsync("replacement");
  texture.colorSpace = SRGBColorSpace;
  const scale = 1;

  const scene = new Scene();
  const targetAspectRatio = targetPainting.width / targetPainting.height;
  const textureAspectRatio = texture.image.width / texture.image.height;
  const [width, height] = targetAspectRatio < textureAspectRatio
    ? [1, 1 / textureAspectRatio]
    : [textureAspectRatio / targetAspectRatio, 1 / targetAspectRatio];
  const geometry = new PlaneGeometry(width * scale, height * scale);
  geometry.rotateX(-Math.PI / 2);
  const replacementPainting = new Mesh(geometry, new MeshBasicMaterial({ map: texture }));
  replacementPainting.matrixAutoUpdate = false;
  replacementPainting.visible = false;
  scene.add(replacementPainting);

  document.getElementById("ar-not-supported")!.style.display = "none";
  const viewInArButton = document.getElementById("view-in-ar") as HTMLButtonElement;
  viewInArButton.style.display = "block"

  viewInArButton.addEventListener("click", async () => {
    const arSession = await navigator.xr!.requestSession("immersive-ar", {
      requiredFeatures: ["image-tracking"],
      trackedImages: [
        {
          image: targetPainting,
          widthInMeters: 1,
        },
      ],
    });
    renderer.xr.setReferenceSpaceType("local");
    await renderer.xr.setSession(arSession);

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
});
