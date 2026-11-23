/**
 * qr.mjs — ES module wrapper around the classic-script qr.js.
 *
 * Use this when you want clean `import` syntax:
 *
 *     import { QRCode } from './qr.mjs'
 *     // or:  import QRCode from './qr.mjs'
 *
 * The actual implementation lives in qr.js (a dependency-free classic script
 * that also works with a plain <script src="qr.js"></script> tag, including
 * over file://). This wrapper simply re-exports the API as named + default
 * ES module bindings.
 */

import './qr.js'

export const QRCode = globalThis.QRCode
export default QRCode
