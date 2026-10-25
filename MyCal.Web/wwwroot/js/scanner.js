let controls, stream;
let generation = 0;
export function stop() {
  generation++;
  controls?.stop();
  controls = undefined;
  stream?.getTracks().forEach((track) => track.stop());
  stream = undefined;
  document.querySelectorAll(".scanner video").forEach((video) => {
    video.srcObject?.getTracks().forEach((track) => track.stop());
    video.srcObject = null;
  });
}
export async function start(id, callback) {
  stop();
  const current = generation;
  if (!navigator.mediaDevices?.getUserMedia)
    throw new Error("Camera access requires HTTPS or localhost.");
  if (!globalThis.ZXingBrowser) await import("/vendor/zxing-browser.min.js");
  if (current !== generation) return;
  const reader = new globalThis.ZXingBrowser.BrowserMultiFormatOneDReader();
  let found = false;
  try {
    const opened = await reader.decodeFromConstraints(
      { audio: false, video: { facingMode: { ideal: "environment" } } },
      id,
      (result) => {
        if (!result || found) return;
        const value = result.getText();
        if (!/^(?:\d{8}|\d{12}|\d{13})$/.test(value)) return;
        found = true;
        stop();
        callback.invokeMethodAsync("BarcodeDetected", value).catch(() => {});
      },
    );
    if (current !== generation || found) {
      opened.stop();
      return;
    }
    controls = opened;
    stream = document.getElementById(id)?.srcObject;
    if (found) stop();
  } catch (error) {
    stop();
    throw error;
  }
}
window.addEventListener("pagehide", stop);
document.addEventListener("visibilitychange", () => {
  if (document.hidden) stop();
});
