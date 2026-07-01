export function WarehouseIllustration() {
  return (
    <svg
      viewBox="0 0 400 300"
      className="w-full h-auto max-w-sm mx-auto"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
    >
      {/* Background */}
      <rect width="400" height="300" fill="transparent" />

      {/* Building */}
      <rect x="50" y="100" width="300" height="150" fill="#E0E7FF" stroke="#A5B4FC" strokeWidth="2" />

      {/* Door */}
      <rect x="175" y="200" width="50" height="50" fill="#818CF8" />
      <circle cx="220" cy="225" r="3" fill="#FEF3C7" />

      {/* Windows Grid */}
      {[0, 1, 2].map((row) =>
        [0, 1, 2, 3].map((col) => (
          <g key={`window-${row}-${col}`}>
            <rect
              x={70 + col * 60}
              y={120 + row * 30}
              width="20"
              height="20"
              fill="#C7D2FE"
              stroke="#A5B4FC"
              strokeWidth="1"
            />
            <line
              x1={80 + col * 60}
              y1={120 + row * 30}
              x2={80 + col * 60}
              y2={140 + row * 30}
              stroke="#A5B4FC"
              strokeWidth="1"
            />
            <line
              x1={70 + col * 60}
              y1={130 + row * 30}
              x2={90 + col * 60}
              y2={130 + row * 30}
              stroke="#A5B4FC"
              strokeWidth="1"
            />
          </g>
        )),
      )}

      {/* Roof */}
      <polygon points="50,100 200,50 350,100" fill="#818CF8" />

      {/* Roof line detail */}
      <line x1="50" y1="100" x2="200" y2="50" stroke="#6366F1" strokeWidth="2" />
      <line x1="200" y1="50" x2="350" y2="100" stroke="#6366F1" strokeWidth="2" />

      {/* Shelving inside (abstracted) */}
      <g opacity="0.6">
        <line x1="70" y1="180" x2="330" y2="180" stroke="#A5B4FC" strokeWidth="1" />
        <line x1="70" y1="210" x2="330" y2="210" stroke="#A5B4FC" strokeWidth="1" />
      </g>

      {/* Packages/Boxes inside */}
      {[0, 1].map((row) =>
        [0, 1, 2].map((col) => (
          <rect
            key={`box-${row}-${col}`}
            x={80 + col * 70}
            y={160 + row * 20}
            width="15"
            height="12"
            fill="#FCD34D"
            stroke="#FBBF24"
            strokeWidth="1"
            opacity="0.8"
          />
        )),
      )}

      {/* Ground */}
      <line x1="0" y1="250" x2="400" y2="250" stroke="#A5B4FC" strokeWidth="2" />
      <rect x="0" y="250" width="400" height="50" fill="#E0E7FF" opacity="0.3" />

      {/* Decorative elements */}
      <circle cx="80" cy="70" r="4" fill="#FCA5A5" opacity="0.6" />
      <circle cx="320" cy="80" r="3" fill="#FCA5A5" opacity="0.6" />
    </svg>
  )
}
