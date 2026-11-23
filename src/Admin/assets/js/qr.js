/**
 * qr.js — a dependency-free QR Code generator for the browser.
 *
 * Implements ISO/IEC 18004 (QR Code), versions 1–40, byte mode (UTF-8),
 * all four error-correction levels, with automatic version and mask selection.
 * No external dependencies, no build step.
 *
 * ---------------------------------------------------------------------------
 * Usage
 * ---------------------------------------------------------------------------
 *   As a classic script (works over http(s) AND file:// — just double-click):
 *       <script src="./qr.js"></script>
 *       // ...then use the global `QRCode`
 *
 *   As an ES module (use the qr.mjs wrapper):
 *       import { QRCode } from './qr.mjs'
 *       // or:  import QRCode from './qr.mjs'
 *
 *   In Node (CommonJS):
 *       const QRCode = require('./qr.js')
 *
 *   const svg     = QRCode.toSVG('https://example.com')
 *   const svgStr  = QRCode.toSVGString('https://example.com')
 *   const canvas  = QRCode.toCanvas('https://example.com')
 *   const dataUrl = QRCode.toDataURL('https://example.com')
 *   const matrix  = QRCode.getMatrix('https://example.com')
 *
 *   Options (all optional):
 *       {
 *         errorCorrectionLevel: 'M',   // 'L' | 'M' | 'Q' | 'H'  (default 'M')
 *         scale: 4,                    // pixel size of one module   (default 4)
 *         margin: 4,                   // quiet-zone modules          (default 4)
 *         dark: '#000000',             // dark module colour          (default '#000000')
 *         light: '#ffffff',            // background colour           (default '#ffffff')
 *         version: 4,                  // force a specific version    (optional)
 *         maskPattern: 2               // force a specific mask 0-7   (optional)
 *       }
 *
 * ---------------------------------------------------------------------------
 * The word "QR Code" is a registered trademark of DENSO WAVE INCORPORATED.
 * This implementation is written from the public specification.
 * ---------------------------------------------------------------------------
 */

'use strict'

// ===========================================================================
// Galois Field GF(2^8)  —  primitive polynomial 0x11d
// ===========================================================================
const EXP = new Uint8Array(512)
const LOG = new Uint8Array(256)

;(function initGaloisField () {
  let x = 1
  for (let i = 0; i < 255; i++) {
    EXP[i] = x
    LOG[x] = i
    x <<= 1
    if (x & 0x100) x ^= 0x11d
  }
  // Double the anti-log table so multiplication never needs a mod 255.
  for (let i = 255; i < 512; i++) {
    EXP[i] = EXP[i - 255]
  }
})()

function gfMul (a, b) {
  if (a === 0 || b === 0) return 0
  return EXP[LOG[a] + LOG[b]]
}

// ===========================================================================
// Reed-Solomon error correction
// ===========================================================================

/**
 * Build the generator polynomial of the given degree.
 * Returns an array of coefficients, index 0 = leading (x^degree) term.
 */
function rsGeneratorPoly (degree) {
  let poly = [1]
  for (let i = 0; i < degree; i++) {
    const next = new Array(poly.length + 1).fill(0)
    for (let j = 0; j < poly.length; j++) {
      next[j] ^= poly[j]                    // poly * x
      next[j + 1] ^= gfMul(poly[j], EXP[i]) // poly * α^i
    }
    poly = next
  }
  return poly
}

/**
 * Compute the `ecLen` Reed-Solomon error-correction codewords for `data`.
 */
function rsEncode (data, ecLen) {
  const gen = rsGeneratorPoly(ecLen)
  const result = new Uint8Array(data.length + ecLen)
  result.set(data)

  for (let i = 0; i < data.length; i++) {
    const factor = result[i]
    if (factor === 0) continue
    for (let j = 1; j < gen.length; j++) {
      result[i + j] ^= gfMul(gen[j], factor)
    }
  }

  return result.slice(data.length)
}

// ===========================================================================
// Version / capacity tables (ISO/IEC 18004)
// ===========================================================================

// Total codewords (data + EC) per version, index 0 unused.
const TOTAL_CODEWORDS = [
  0,
  26, 44, 70, 100, 134, 172, 196, 242, 292, 346,
  404, 466, 532, 581, 655, 733, 815, 901, 991, 1085,
  1156, 1258, 1364, 1474, 1588, 1706, 1828, 1921, 2051, 2185,
  2323, 2465, 2611, 2761, 2876, 3034, 3196, 3362, 3532, 3706
]

// Number of EC blocks per version, per EC level [L, M, Q, H].
const EC_BLOCKS = [
  [1, 1, 1, 1],    // v1
  [1, 1, 1, 1],    // v2
  [1, 1, 2, 2],    // v3
  [1, 2, 2, 4],    // v4
  [1, 2, 4, 4],    // v5
  [2, 4, 4, 4],    // v6
  [2, 4, 6, 5],    // v7
  [2, 4, 6, 6],    // v8
  [2, 5, 8, 8],    // v9
  [4, 5, 8, 8],    // v10
  [4, 5, 8, 11],   // v11
  [4, 8, 10, 11],  // v12
  [4, 9, 12, 16],  // v13
  [4, 9, 16, 16],  // v14
  [6, 10, 12, 18], // v15
  [6, 10, 17, 16], // v16
  [6, 11, 16, 19], // v17
  [6, 13, 18, 21], // v18
  [7, 14, 21, 25], // v19
  [8, 16, 20, 25], // v20
  [8, 17, 23, 25], // v21
  [9, 17, 23, 34], // v22
  [9, 18, 25, 30], // v23
  [10, 20, 27, 32],// v24
  [12, 21, 29, 35],// v25
  [12, 23, 34, 37],// v26
  [12, 25, 34, 40],// v27
  [13, 26, 35, 42],// v28
  [14, 28, 38, 45],// v29
  [15, 29, 40, 48],// v30
  [16, 31, 43, 51],// v31
  [17, 33, 45, 54],// v32
  [18, 35, 48, 57],// v33
  [19, 37, 51, 60],// v34
  [19, 38, 53, 63],// v35
  [20, 40, 56, 66],// v36
  [21, 43, 59, 70],// v37
  [22, 45, 62, 74],// v38
  [24, 47, 65, 77],// v39
  [25, 49, 68, 81] // v40
]

// Total EC codewords per version, per EC level [L, M, Q, H].
const EC_CODEWORDS = [
  [7, 10, 13, 17],    // v1
  [10, 16, 22, 28],   // v2
  [15, 26, 36, 44],   // v3
  [20, 36, 52, 64],   // v4
  [26, 48, 72, 88],   // v5
  [36, 64, 96, 112],  // v6
  [40, 72, 108, 130], // v7
  [48, 88, 132, 156], // v8
  [60, 110, 160, 192],// v9
  [72, 130, 192, 224],// v10
  [80, 150, 224, 264],// v11
  [96, 176, 260, 308],// v12
  [104, 198, 288, 352],// v13
  [120, 216, 320, 384],// v14
  [132, 240, 360, 432],// v15
  [144, 280, 408, 480],// v16
  [168, 308, 448, 532],// v17
  [180, 338, 504, 588],// v18
  [196, 364, 546, 650],// v19
  [224, 416, 600, 700],// v20
  [224, 442, 644, 750],// v21
  [252, 476, 690, 816],// v22
  [270, 504, 750, 900],// v23
  [300, 560, 810, 960],// v24
  [312, 588, 870, 1050],// v25
  [336, 644, 952, 1110],// v26
  [360, 700, 1020, 1200],// v27
  [390, 728, 1050, 1260],// v28
  [420, 784, 1140, 1350],// v29
  [450, 812, 1200, 1440],// v30
  [480, 868, 1290, 1530],// v31
  [510, 924, 1350, 1620],// v32
  [540, 980, 1440, 1710],// v33
  [570, 1036, 1530, 1800],// v34
  [570, 1064, 1590, 1890],// v35
  [600, 1120, 1680, 1980],// v36
  [630, 1204, 1770, 2100],// v37
  [660, 1260, 1860, 2220],// v38
  [720, 1316, 1950, 2310],// v39
  [750, 1372, 2040, 2430]// v40
]

// Error-correction level bit values (2 bits each).
const EC_LEVELS = { L: 1, M: 0, Q: 3, H: 2 }
const EC_LEVEL_INDEX = { L: 0, M: 1, Q: 2, H: 3 }

// ===========================================================================
// Bit buffer (MSB-first)
// ===========================================================================
function BitBuffer () {
  this.bytes = []
  this.length = 0
}

BitBuffer.prototype.put = function (num, len) {
  for (let i = 0; i < len; i++) {
    this.putBit(((num >>> (len - i - 1)) & 1) === 1)
  }
}

BitBuffer.prototype.putBit = function (bit) {
  const byteIndex = this.length >>> 3
  if (this.bytes.length <= byteIndex) this.bytes.push(0)
  if (bit) this.bytes[byteIndex] |= 0x80 >>> (this.length % 8)
  this.length++
}

BitBuffer.prototype.toBytes = function () {
  return new Uint8Array(this.bytes)
}

// ===========================================================================
// Symbol geometry helpers
// ===========================================================================
function symbolSize (version) {
  return version * 4 + 17
}

/**
 * Row/column coordinates of the centre of each alignment pattern.
 */
function alignmentCoords (version) {
  if (version === 1) return []
  const posCount = Math.floor(version / 7) + 2
  const size = symbolSize(version)
  const step = size === 145 ? 26 : Math.ceil((size - 13) / (2 * posCount - 2)) * 2
  const positions = [size - 7]
  for (let i = 1; i < posCount - 1; i++) {
    positions[i] = positions[i - 1] - step
  }
  positions.push(6)
  return positions.reverse()
}

/**
 * (row, col) centres of every alignment pattern, skipping the three corners
 * that overlap the finder patterns.
 */
function alignmentPositions (version) {
  const coords = alignmentCoords(version)
  const n = coords.length
  const out = []
  for (let i = 0; i < n; i++) {
    for (let j = 0; j < n; j++) {
      if ((i === 0 && j === 0) || (i === 0 && j === n - 1) || (i === n - 1 && j === 0)) continue
      out.push([coords[i], coords[j]])
    }
  }
  return out
}

// ===========================================================================
// BCH codes (format + version information)
// ===========================================================================
function bchDigit (data) {
  let digit = 0
  while (data !== 0) {
    digit++
    data >>>= 1
  }
  return digit
}

const FORMAT_G15 = (1 << 10) | (1 << 8) | (1 << 5) | (1 << 4) | (1 << 2) | (1 << 1) | (1 << 0)
const FORMAT_G15_BCH = bchDigit(FORMAT_G15)
const FORMAT_MASK = (1 << 14) | (1 << 12) | (1 << 10) | (1 << 4) | (1 << 1)

/**
 * 15-bit format information (5 data bits + 10 BCH) XORed with the fixed mask.
 */
function formatInfoBits (ecLevel, mask) {
  const data = (ecLevel << 3) | mask
  let d = data << 10
  while (bchDigit(d) - FORMAT_G15_BCH >= 0) {
    d ^= FORMAT_G15 << (bchDigit(d) - FORMAT_G15_BCH)
  }
  return ((data << 10) | d) ^ FORMAT_MASK
}

const VERSION_G18 =
  (1 << 12) | (1 << 11) | (1 << 10) | (1 << 9) | (1 << 8) | (1 << 5) | (1 << 2) | (1 << 0)
const VERSION_G18_BCH = bchDigit(VERSION_G18)

/**
 * 18-bit version information (6 data bits + 12 BCH). Only for version >= 7.
 */
function versionInfoBits (version) {
  let d = version << 12
  while (bchDigit(d) - VERSION_G18_BCH >= 0) {
    d ^= VERSION_G18 << (bchDigit(d) - VERSION_G18_BCH)
  }
  return (version << 12) | d
}

// ===========================================================================
// Matrix assembly
// ===========================================================================

/**
 * A QR symbol: a flat `size x size` module grid plus a "reserved" mask
 * (function patterns) that must not be touched by data or masking.
 */
function Matrix (size) {
  this.size = size
  this.data = new Uint8Array(size * size)
  this.reserved = new Uint8Array(size * size)
}

Matrix.prototype.set = function (row, col, value, reserved) {
  const i = row * this.size + col
  this.data[i] = value ? 1 : 0
  if (reserved) this.reserved[i] = 1
}

Matrix.prototype.get = function (row, col) {
  return this.data[row * this.size + col]
}

Matrix.prototype.xor = function (row, col, value) {
  this.data[row * this.size + col] ^= value ? 1 : 0
}

Matrix.prototype.isReserved = function (row, col) {
  return this.reserved[row * this.size + col] === 1
}

function setupFinderPattern (matrix) {
  const size = matrix.size
  const corners = [[0, 0], [0, size - 7], [size - 7, 0]]
  for (const [top, left] of corners) {
    for (let r = -1; r <= 7; r++) {
      for (let c = -1; c <= 7; c++) {
        const row = top + r
        const col = left + c
        if (row < 0 || row >= size || col < 0 || col >= size) continue
        const dark =
          (r >= 0 && r <= 6 && (c === 0 || c === 6)) ||
          (c >= 0 && c <= 6 && (r === 0 || r === 6)) ||
          (r >= 2 && r <= 4 && c >= 2 && c <= 4)
        matrix.set(row, col, dark, true)
      }
    }
  }
}

function setupTimingPattern (matrix) {
  const size = matrix.size
  for (let i = 8; i < size - 8; i++) {
    const value = i % 2 === 0
    matrix.set(i, 6, value, true)
    matrix.set(6, i, value, true)
  }
}

function setupAlignmentPattern (matrix, version) {
  for (const [row, col] of alignmentPositions(version)) {
    for (let r = -2; r <= 2; r++) {
      for (let c = -2; c <= 2; c++) {
        const dark = r === -2 || r === 2 || c === -2 || c === 2 || (r === 0 && c === 0)
        matrix.set(row + r, col + c, dark, true)
      }
    }
  }
}

function setupDarkModule (matrix) {
  // The single always-dark module (part of the format information area).
  matrix.set(matrix.size - 8, 8, true, true)
}

function setupFormatInfo (matrix, ecLevel, mask) {
  const size = matrix.size
  const bits = formatInfoBits(EC_LEVELS[ecLevel], mask)

  for (let i = 0; i < 15; i++) {
    const mod = ((bits >> i) & 1) === 1
    // vertical strip (column 8)
    if (i < 6) matrix.set(i, 8, mod, true)
    else if (i < 8) matrix.set(i + 1, 8, mod, true)
    else matrix.set(size - 15 + i, 8, mod, true)

    // horizontal strip (row 8)
    if (i < 8) matrix.set(8, size - i - 1, mod, true)
    else if (i < 9) matrix.set(8, 15 - i, mod, true)
    else matrix.set(8, 15 - i - 1, mod, true)
  }

  setupDarkModule(matrix)
}

function setupVersionInfo (matrix, version) {
  if (version < 7) return
  const size = matrix.size
  const bits = versionInfoBits(version)
  for (let i = 0; i < 18; i++) {
    const mod = ((bits >> i) & 1) === 1
    const row = Math.floor(i / 3)
    const col = (i % 3) + size - 8 - 3
    matrix.set(row, col, mod, true)
    matrix.set(col, row, mod, true)
  }
}

/**
 * Place the interleaved codewords into the symbol using the standard
 * right-to-left, bottom-to-top zig-zag, skipping reserved modules.
 */
function setupData (matrix, codewords) {
  const size = matrix.size
  let inc = -1
  let row = size - 1
  let bitIndex = 7
  let byteIndex = 0

  for (let col = size - 1; col > 0; col -= 2) {
    if (col === 6) col--

    while (true) {
      for (let c = 0; c < 2; c++) {
        if (!matrix.isReserved(row, col - c)) {
          let dark = false
          if (byteIndex < codewords.length) {
            dark = (((codewords[byteIndex] >>> bitIndex) & 1) === 1)
          }
          matrix.set(row, col - c, dark)
          bitIndex--
          if (bitIndex === -1) {
            byteIndex++
            bitIndex = 7
          }
        }
      }

      row += inc
      if (row < 0 || row >= size) {
        row -= inc
        inc = -inc
        break
      }
    }
  }
}

// ===========================================================================
// Data encoding (byte mode) + Reed-Solomon + interleaving
// ===========================================================================

/**
 * Number of bits for the byte-mode character-count indicator.
 */
function byteCountBits (version) {
  return version < 10 ? 8 : 16
}

/**
 * Maximum number of data bytes a (version, ecLevel) symbol can hold in byte mode.
 */
function byteCapacity (version, ecLevel) {
  const total = TOTAL_CODEWORDS[version]
  const ec = EC_CODEWORDS[version - 1][EC_LEVEL_INDEX[ecLevel]]
  const dataBits = (total - ec) * 8
  return Math.floor((dataBits - 4 - byteCountBits(version)) / 8)
}

/**
 * Smallest version (1–40) that can hold `byteLength` bytes at `ecLevel`,
 * or null if none fits.
 */
function bestVersion (byteLength, ecLevel) {
  for (let v = 1; v <= 40; v++) {
    if (byteLength <= byteCapacity(v, ecLevel)) return v
  }
  return null
}

/**
 * Build the full codeword sequence (data + interleaved EC) for the payload.
 */
function buildCodewords (utf8Bytes, version, ecLevel) {
  const buffer = new BitBuffer()

  // Mode indicator (byte mode = 0100) + character count.
  buffer.put(0b0100, 4)
  buffer.put(utf8Bytes.length, byteCountBits(version))

  // Payload.
  for (let i = 0; i < utf8Bytes.length; i++) {
    buffer.put(utf8Bytes[i], 8)
  }

  // Terminator (up to 4 zero bits).
  const total = TOTAL_CODEWORDS[version]
  const ecTotal = EC_CODEWORDS[version - 1][EC_LEVEL_INDEX[ecLevel]]
  const dataCapacityBits = (total - ecTotal) * 8
  if (buffer.length + 4 <= dataCapacityBits) buffer.put(0, 4)

  // Pad to a byte boundary.
  while (buffer.length % 8 !== 0) buffer.putBit(0)

  // Pad with alternating 0xEC / 0x11 to fill the data capacity.
  let pad = 0xec
  while (buffer.length < dataCapacityBits) {
    buffer.put(pad, 8)
    pad = pad === 0xec ? 0x11 : 0xec
  }

  const dataBytes = buffer.toBytes()
  const dataTotal = total - ecTotal
  const blockCount = EC_BLOCKS[version - 1][EC_LEVEL_INDEX[ecLevel]]

  // Split into blocks (two sizes at most).
  const blocksInGroup2 = total % blockCount
  const blocksInGroup1 = blockCount - blocksInGroup2
  const dataPerBlock1 = Math.floor(dataTotal / blockCount)
  const dataPerBlock2 = dataPerBlock1 + 1
  const ecPerBlock = Math.floor(total / blockCount) - dataPerBlock1

  const dataBlocks = []
  const ecBlocks = []
  let offset = 0
  for (let b = 0; b < blockCount; b++) {
    const size = b < blocksInGroup1 ? dataPerBlock1 : dataPerBlock2
    const block = dataBytes.slice(offset, offset + size)
    dataBlocks.push(block)
    ecBlocks.push(rsEncode(block, ecPerBlock))
    offset += size
  }

  // Interleave data codewords, then EC codewords.
  const maxData = Math.max(dataPerBlock1, dataPerBlock2)
  const out = new Uint8Array(total)
  let index = 0
  for (let i = 0; i < maxData; i++) {
    for (let r = 0; r < blockCount; r++) {
      if (i < dataBlocks[r].length) out[index++] = dataBlocks[r][i]
    }
  }
  for (let i = 0; i < ecPerBlock; i++) {
    for (let r = 0; r < blockCount; r++) {
      out[index++] = ecBlocks[r][i]
    }
  }

  return out
}

// ===========================================================================
// Masking
// ===========================================================================
const PENALTY = { N1: 3, N2: 3, N3: 40, N4: 10 }

function maskAt (pattern, row, col) {
  switch (pattern) {
    case 0: return (row + col) % 2 === 0
    case 1: return row % 2 === 0
    case 2: return col % 3 === 0
    case 3: return (row + col) % 3 === 0
    case 4: return (Math.floor(row / 2) + Math.floor(col / 3)) % 2 === 0
    case 5: return ((row * col) % 2) + ((row * col) % 3) === 0
    case 6: return (((row * col) % 2) + ((row * col) % 3)) % 2 === 0
    case 7: return (((row * col) % 3) + ((row + col) % 2)) % 2 === 0
    default: throw new Error('Invalid mask pattern: ' + pattern)
  }
}

function applyMask (pattern, matrix) {
  const size = matrix.size
  for (let row = 0; row < size; row++) {
    for (let col = 0; col < size; col++) {
      if (matrix.isReserved(row, col)) continue
      matrix.xor(row, col, maskAt(pattern, row, col))
    }
  }
}

function penaltyN1 (matrix) {
  const size = matrix.size
  let points = 0
  for (let row = 0; row < size; row++) {
    let runCol = 0
    let runRow = 0
    let lastCol = null
    let lastRow = null
    for (let col = 0; col < size; col++) {
      let m = matrix.get(row, col)
      if (m === lastCol) runCol++
      else {
        if (runCol >= 5) points += PENALTY.N1 + (runCol - 5)
        lastCol = m
        runCol = 1
      }

      m = matrix.get(col, row)
      if (m === lastRow) runRow++
      else {
        if (runRow >= 5) points += PENALTY.N1 + (runRow - 5)
        lastRow = m
        runRow = 1
      }
    }
    if (runCol >= 5) points += PENALTY.N1 + (runCol - 5)
    if (runRow >= 5) points += PENALTY.N1 + (runRow - 5)
  }
  return points
}

function penaltyN2 (matrix) {
  const size = matrix.size
  let points = 0
  for (let row = 0; row < size - 1; row++) {
    for (let col = 0; col < size - 1; col++) {
      const sum =
        matrix.get(row, col) +
        matrix.get(row, col + 1) +
        matrix.get(row + 1, col) +
        matrix.get(row + 1, col + 1)
      if (sum === 4 || sum === 0) points++
    }
  }
  return points * PENALTY.N2
}

function penaltyN3 (matrix) {
  const size = matrix.size
  let points = 0
  for (let row = 0; row < size; row++) {
    let bitsCol = 0
    let bitsRow = 0
    for (let col = 0; col < size; col++) {
      bitsCol = ((bitsCol << 1) & 0x7ff) | matrix.get(row, col)
      if (col >= 10 && (bitsCol === 0x5d0 || bitsCol === 0x05d)) points++

      bitsRow = ((bitsRow << 1) & 0x7ff) | matrix.get(col, row)
      if (col >= 10 && (bitsRow === 0x5d0 || bitsRow === 0x05d)) points++
    }
  }
  return points * PENALTY.N3
}

function penaltyN4 (matrix) {
  let dark = 0
  const total = matrix.data.length
  for (let i = 0; i < total; i++) dark += matrix.data[i]
  const k = Math.abs(Math.ceil((dark * 100 / total) / 5) - 10)
  return k * PENALTY.N4
}

function totalPenalty (matrix) {
  return penaltyN1(matrix) + penaltyN2(matrix) + penaltyN3(matrix) + penaltyN4(matrix)
}

/**
 * Choose the mask pattern with the lowest penalty score.
 */
function chooseBestMask (matrix, ecLevel) {
  let best = 0
  let lowest = Infinity
  for (let p = 0; p < 8; p++) {
    setupFormatInfo(matrix, ecLevel, p)
    applyMask(p, matrix)
    const penalty = totalPenalty(matrix)
    applyMask(p, matrix) // undo
    if (penalty < lowest) {
      lowest = penalty
      best = p
    }
  }
  return best
}

// ===========================================================================
// Core create()
// ===========================================================================

function toUTF8 (text) {
  // TextEncoder is available in all modern browsers and Node.
  if (typeof TextEncoder !== 'undefined') {
    return new TextEncoder().encode(text)
  }
  // Fallback: manual UTF-8 encoding.
  const out = []
  for (let i = 0; i < text.length; i++) {
    let code = text.charCodeAt(i)
    if (code >= 0xd800 && code <= 0xdbff && i + 1 < text.length) {
      const low = text.charCodeAt(i + 1)
      if (low >= 0xdc00 && low <= 0xdfff) {
        code = 0x10000 + ((code - 0xd800) << 10) + (low - 0xdc00)
        i++
      }
    }
    if (code < 0x80) out.push(code)
    else if (code < 0x800) {
      out.push(0xc0 | (code >> 6), 0x80 | (code & 0x3f))
    } else if (code < 0x10000) {
      out.push(0xe0 | (code >> 12), 0x80 | ((code >> 6) & 0x3f), 0x80 | (code & 0x3f))
    } else {
      out.push(
        0xf0 | (code >> 18),
        0x80 | ((code >> 12) & 0x3f),
        0x80 | ((code >> 6) & 0x3f),
        0x80 | (code & 0x3f)
      )
    }
  }
  return new Uint8Array(out)
}

function normalizeOptions (opts) {
  opts = opts || {}
  const ecLevel = EC_LEVELS[opts.errorCorrectionLevel] !== undefined
    ? opts.errorCorrectionLevel
    : 'M'
  return {
    errorCorrectionLevel: ecLevel,
    scale: opts.scale !== undefined ? opts.scale : 4,
    margin: opts.margin !== undefined ? opts.margin : 4,
    dark: opts.dark || '#000000',
    light: opts.light || '#ffffff',
    version: opts.version !== undefined && opts.version !== null ? opts.version : null,
    maskPattern: opts.maskPattern !== undefined && opts.maskPattern !== null ? opts.maskPattern : null
  }
}

/**
 * Generate a QR symbol for the given text.
 *
 * @returns {{size:number, modules:Uint8Array, version:number,
 *            errorCorrectionLevel:string, maskPattern:number}}
 */
function create (text, opts) {
  if (typeof text !== 'string' || text.length === 0) {
    throw new Error('QRCode: input text is required')
  }

  const options = normalizeOptions(opts)
  const ecLevel = options.errorCorrectionLevel
  const utf8 = toUTF8(text)

  // Determine version.
  let version = options.version
  if (version === null) {
    version = bestVersion(utf8.length, ecLevel)
    if (!version) {
      throw new Error('QRCode: data is too large for a QR code (max version 40)')
    }
  } else if (!Number.isInteger(version) || version < 1 || version > 40) {
    throw new Error('QRCode: version must be an integer between 1 and 40')
  } else if (utf8.length > byteCapacity(version, ecLevel)) {
    throw new Error(
      'QRCode: data does not fit in version ' + version + ' at EC level ' + ecLevel
    )
  }

  // Build codewords.
  const codewords = buildCodewords(utf8, version, ecLevel)

  // Build the symbol.
  const size = symbolSize(version)
  const matrix = new Matrix(size)

  setupFinderPattern(matrix)
  setupTimingPattern(matrix)
  setupAlignmentPattern(matrix, version)
  setupFormatInfo(matrix, ecLevel, 0) // placeholder (reserved) bits
  setupVersionInfo(matrix, version)
  setupData(matrix, codewords)

  // Choose + apply the mask.
  let mask = options.maskPattern
  if (mask === null) {
    mask = chooseBestMask(matrix, ecLevel)
  } else if (mask < 0 || mask > 7) {
    throw new Error('QRCode: maskPattern must be between 0 and 7')
  }
  applyMask(mask, matrix)

  // Write the final format information.
  setupFormatInfo(matrix, ecLevel, mask)

  return {
    size,
    modules: matrix.data,
    version,
    errorCorrectionLevel: ecLevel,
    maskPattern: mask
  }
}

// ===========================================================================
// Renderers
// ===========================================================================

function buildPath (symbol, margin) {
  const { size, modules } = symbol
  let d = ''
  for (let row = 0; row < size; row++) {
    for (let col = 0; col < size; col++) {
      if (modules[row * size + col]) {
        d += 'M' + (col + margin) + ' ' + (row + margin) + 'h1v1h-1z'
      }
    }
  }
  return d
}

function toSVGString (text, opts) {
  const options = normalizeOptions(opts)
  const symbol = create(text, options)
  const { size } = symbol
  const margin = options.margin
  const n = size + margin * 2
  const scale = options.scale

  const width = n * scale
  const d = buildPath(symbol, margin)

  return (
    '<svg xmlns="http://www.w3.org/2000/svg" width="' + width + '" height="' + width + '" ' +
    'viewBox="0 0 ' + n + ' ' + n + '" shape-rendering="crispEdges">' +
    '<rect width="' + n + '" height="' + n + '" fill="' + options.light + '"/>' +
    '<path fill="' + options.dark + '" d="' + d + '"/>' +
    '</svg>'
  )
}

function toSVG (text, opts) {
  const svgString = toSVGString(text, opts)
  if (typeof DOMParser === 'undefined') {
    throw new Error('QRCode.toSVG requires a DOM (browser environment)')
  }
  const doc = new DOMParser().parseFromString(svgString, 'image/svg+xml')
  return doc.documentElement
}

function toCanvas (text, opts) {
  const options = normalizeOptions(opts)
  const symbol = create(text, options)
  const { size, modules } = symbol
  const margin = options.margin
  const scale = options.scale
  const n = size + margin * 2

  if (typeof document === 'undefined') {
    throw new Error('QRCode.toCanvas requires a DOM (browser environment)')
  }

  const canvas = document.createElement('canvas')
  canvas.width = n * scale
  canvas.height = n * scale
  const ctx = canvas.getContext('2d')

  ctx.fillStyle = options.light
  ctx.fillRect(0, 0, canvas.width, canvas.height)

  ctx.fillStyle = options.dark
  for (let row = 0; row < size; row++) {
    for (let col = 0; col < size; col++) {
      if (modules[row * size + col]) {
        ctx.fillRect((col + margin) * scale, (row + margin) * scale, scale, scale)
      }
    }
  }

  return canvas
}

function toDataURL (text, opts) {
  return toCanvas(text, opts).toDataURL('image/png')
}

// ===========================================================================
// Public API
// ===========================================================================
const QRCode = {
  create,
  getMatrix: create,
  toSVGString,
  toSVG,
  toCanvas,
  toDataURL,
  // Exposed for testing / advanced use.
  _internals: {
    rsEncode,
    rsGeneratorPoly,
    formatInfoBits,
    versionInfoBits,
    byteCapacity,
    bestVersion,
    alignmentPositions,
    buildCodewords,
    TOTAL_CODEWORDS,
    EC_BLOCKS,
    EC_CODEWORDS
  }
}

// ---------------------------------------------------------------------------
// Exports (UMD-style): works as a browser global, a CommonJS module, and —
// via the qr.mjs wrapper — an ES module. Keeping this a classic script (no
// top-level `export`) is what allows it to load from a file:// URL with a
// plain <script src="qr.js"></script> tag.
// ---------------------------------------------------------------------------

// Browser global (also available when loaded via <script src="qr.js">).
if (typeof globalThis !== 'undefined') {
  globalThis.QRCode = QRCode
}

// CommonJS (Node).
if (typeof module === 'object' && typeof module.exports === 'object') {
  module.exports = QRCode
  module.exports.default = QRCode
}
