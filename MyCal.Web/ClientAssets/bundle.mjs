import { mkdir, copyFile } from "node:fs/promises";
const source = new URL(".", import.meta.url);
const destination = new URL("../wwwroot/vendor/", source);
await mkdir(destination, { recursive: true });
await copyFile(
  new URL("node_modules/@zxing/browser/umd/zxing-browser.min.js", source),
  new URL("zxing-browser.min.js", destination),
);
await copyFile(
  new URL("node_modules/@zxing/browser/LICENSE", source),
  new URL("ZXING-LICENSE", destination),
);
await copyFile(
  new URL("node_modules/@zxing/library/LICENSE", source),
  new URL("ZXING-LIBRARY-LICENSE", destination),
);
