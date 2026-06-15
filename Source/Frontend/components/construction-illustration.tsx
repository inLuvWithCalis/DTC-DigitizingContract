export function ConstructionIllustration() {
  return (
    <svg
      viewBox="0 0 400 300"
      className="w-full h-auto max-w-sm mx-auto"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
    >
      {/* Background */}
      <defs>
        <linearGradient id="steelGrad" x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" style={{ stopColor: "#1f293e", stopOpacity: 1 }} />
          <stop offset="100%" style={{ stopColor: "#0f172a", stopOpacity: 1 }} />
        </linearGradient>
      </defs>
      <rect width="400" height="300" fill="url(#steelGrad)" />

      {/* Main steel framework - vertical columns */}
      <line x1="80" y1="50" x2="80" y2="240" stroke="#64748b" strokeWidth="8" />
      <line x1="200" y1="50" x2="200" y2="240" stroke="#64748b" strokeWidth="8" />
      <line x1="320" y1="50" x2="320" y2="240" stroke="#64748b" strokeWidth="8" />

      {/* Cross beams - horizontal supports */}
      <line x1="60" y1="90" x2="340" y2="90" stroke="#475569" strokeWidth="6" />
      <line x1="60" y1="140" x2="340" y2="140" stroke="#475569" strokeWidth="6" />
      <line x1="60" y1="190" x2="340" y2="190" stroke="#475569" strokeWidth="6" />
      <line x1="60" y1="240" x2="340" y2="240" stroke="#64748b" strokeWidth="8" />

      {/* Diagonal bracing - structural elements */}
      <line x1="80" y1="90" x2="120" y2="140" stroke="#f97316" strokeWidth="3" opacity="0.7" />
      <line x1="280" y1="90" x2="320" y2="140" stroke="#f97316" strokeWidth="3" opacity="0.7" />
      <line x1="80" y1="140" x2="120" y2="190" stroke="#f97316" strokeWidth="3" opacity="0.7" />
      <line x1="280" y1="140" x2="320" y2="190" stroke="#f97316" strokeWidth="3" opacity="0.7" />

      {/* Diagonal braces for reinforcement */}
      <line x1="120" y1="90" x2="80" y2="140" stroke="#94a3b8" strokeWidth="2" opacity="0.6" />
      <line x1="280" y1="90" x2="320" y2="140" stroke="#94a3b8" strokeWidth="2" opacity="0.6" />

      {/* Material storage shelving indicator */}
      <g opacity="0.8">
        {/* Shelf levels */}
        {[100, 150, 200].map((y) => (
          <g key={`shelf-${y}`}>
            <rect x="120" y={y - 8} width="160" height="4" fill="#cbd5e1" />
            {/* Stored materials representation */}
            <rect x="130" y={y - 20} width="12" height="18" fill="#fbbf24" opacity="0.8" />
            <rect x="150" y={y - 22} width="12" height="20" fill="#f97316" opacity="0.7" />
            <rect x="170" y={y - 18} width="12" height="16" fill="#fbbf24" opacity="0.8" />
            <rect x="190" y={y - 20} width="12" height="18" fill="#fb923c" opacity="0.75" />
            <rect x="210" y={y - 19} width="12" height="17" fill="#fbbf24" opacity="0.8" />
            <rect x="230" y={y - 21} width="12" height="19" fill="#f97316" opacity="0.7" />
          </g>
        ))}
      </g>

      {/* Accent elements - safety markers */}
      <circle cx="380" cy="60" r="6" fill="#f97316" opacity="0.8" />
      <circle cx="20" cy="70" r="5" fill="#f97316" opacity="0.6" />
      <circle cx="390" cy="200" r="5" fill="#f97316" opacity="0.6" />

      {/* Structural joints - welding points */}
      <circle cx="80" cy="90" r="4" fill="#fbbf24" opacity="0.9" />
      <circle cx="200" cy="90" r="4" fill="#fbbf24" opacity="0.9" />
      <circle cx="320" cy="90" r="4" fill="#fbbf24" opacity="0.9" />
      <circle cx="80" cy="140" r="4" fill="#fbbf24" opacity="0.9" />
      <circle cx="200" cy="140" r="4" fill="#fbbf24" opacity="0.9" />
      <circle cx="320" cy="140" r="4" fill="#fbbf24" opacity="0.9" />

      {/* Ground/foundation */}
      <rect x="40" y="240" width="320" height="3" fill="#64748b" />
      <rect x="40" y="243" width="320" height="20" fill="#0f172a" opacity="0.5" />
      <line x1="60" y1="263" x2="340" y2="263" stroke="#475569" strokeWidth="1" opacity="0.5" />
    </svg>
  )
}
