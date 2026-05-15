import { useEffect, useRef, useState } from "react";
import { motion } from "framer-motion";
import { HeartPulse, Mic, Video, PhoneOff, Signal } from "lucide-react";

function useClock() {
  const [now, setNow] = useState(() => new Date());
  useEffect(() => {
    const id = setInterval(() => setNow(new Date()), 1000);
    return () => clearInterval(id);
  }, []);
  return now.toLocaleTimeString("vi-VN", {
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function ConsultCard() {
  const time = useClock();
  const ref = useRef<HTMLDivElement>(null);
  const [tilt, setTilt] = useState({ x: 0, y: 0 });

  const handleMouseMove = (e: React.MouseEvent) => {
    const el = ref.current;
    if (!el) return;
    const rect = el.getBoundingClientRect();
    const px = (e.clientX - rect.left) / rect.width - 0.5;
    const py = (e.clientY - rect.top) / rect.height - 0.5;
    setTilt({ x: -py * 6, y: px * 8 });
  };
  const reset = () => setTilt({ x: 0, y: 0 });

  return (
    <motion.div
      initial={{ opacity: 0, y: 30, scale: 0.96 }}
      animate={{ opacity: 1, y: 0, scale: 1 }}
      transition={{ duration: 0.8, delay: 0.3, ease: [0.22, 1, 0.36, 1] }}
      className="relative"
    >
      {/* Pulse rings behind card */}
      <div className="pointer-events-none absolute inset-0 -z-10 flex items-center justify-center">
        <span
          className="absolute h-72 w-72 rounded-full bg-primary-glow/30"
          style={{ animation: "pulse-ring 3s ease-out infinite" }}
        />
        <span
          className="absolute h-72 w-72 rounded-full bg-primary/20"
          style={{ animation: "pulse-ring 3s ease-out 1.2s infinite" }}
        />
      </div>

      <div
        ref={ref}
        onMouseMove={handleMouseMove}
        onMouseLeave={reset}
        className="animate-float"
        style={{ perspective: "1200px" }}
      >
        <div
          className="relative overflow-hidden rounded-3xl border border-white/60 bg-white/80 p-5 backdrop-blur-xl transition-transform duration-200 will-change-transform"
          style={{
            boxShadow: "var(--shadow-card)",
            transform: `rotateX(${tilt.x}deg) rotateY(${tilt.y}deg)`,
            transformStyle: "preserve-3d",
          }}
        >
          {/* Header */}
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <span className="relative flex h-2.5 w-2.5">
                <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-emerald-400 opacity-75" />
                <span className="relative inline-flex h-2.5 w-2.5 rounded-full bg-emerald-500" />
              </span>
              <span className="text-sm font-semibold text-foreground">
                Phòng khám trực tuyến
              </span>
            </div>
            <span className="font-mono text-xs tabular-nums text-muted-foreground">
              {time}
            </span>
          </div>

          {/* Video tile */}
          <div
            className="relative mt-4 aspect-[4/3] overflow-hidden rounded-2xl"
            style={{
              background:
                "linear-gradient(135deg, oklch(0.35 0.1 230), oklch(0.55 0.12 200))",
            }}
          >
            {/* Soft grid */}
            <div
              className="absolute inset-0 opacity-20"
              style={{
                backgroundImage:
                  "linear-gradient(rgba(255,255,255,0.25) 1px, transparent 1px), linear-gradient(90deg, rgba(255,255,255,0.25) 1px, transparent 1px)",
                backgroundSize: "32px 32px",
              }}
            />

            {/* Doctor avatar with pulse */}
            <div className="absolute inset-0 flex flex-col items-center justify-center text-white">
              <div className="relative">
                <span
                  className="absolute inset-0 rounded-full bg-white/40"
                  style={{ animation: "pulse-ring 2.4s ease-out infinite" }}
                />
                <div className="relative flex h-20 w-20 items-center justify-center rounded-full bg-gradient-to-br from-white to-white/70 text-2xl font-bold text-primary shadow-xl">
                  TA
                </div>
              </div>
              <div className="mt-4 text-center">
                <p className="text-base font-semibold">ThS. BS Trần Minh An</p>
                <p className="text-xs text-white/80">Chuyên khoa Tim mạch</p>
              </div>

              {/* Connecting dots */}
              <div className="mt-3 flex items-center gap-1.5 rounded-full bg-black/25 px-3 py-1 backdrop-blur-sm">
                <span className="text-[11px] font-medium">Đang kết nối</span>
                {[0, 1, 2].map((i) => (
                  <span
                    key={i}
                    className="inline-block h-1.5 w-1.5 rounded-full bg-white"
                    style={{
                      animation: `dot-bounce 1.4s ease-in-out ${i * 0.16}s infinite`,
                    }}
                  />
                ))}
              </div>
            </div>

            {/* PiP self-view */}
            <div className="absolute right-3 top-3 flex h-16 w-20 items-end justify-center rounded-lg bg-gradient-to-br from-slate-700 to-slate-900 shadow-lg ring-1 ring-white/20">
              <div className="mb-1 h-7 w-7 rounded-full bg-gradient-to-br from-orange-300 to-pink-400" />
            </div>

            {/* Signal */}
            <div className="absolute left-3 top-3 flex items-center gap-1 rounded-md bg-black/30 px-1.5 py-0.5 text-[10px] text-white backdrop-blur">
              <Signal className="h-3 w-3" />
              HD
            </div>

            {/* ECG waveform */}
            <div className="absolute bottom-0 left-0 right-0 h-12 bg-gradient-to-t from-black/50 to-transparent">
              <svg
                viewBox="0 0 300 40"
                className="absolute bottom-2 left-0 h-8 w-full"
                preserveAspectRatio="none"
              >
                <path
                  d="M0 20 L40 20 L50 20 L55 8 L62 32 L70 4 L78 28 L85 20 L120 20 L130 20 L138 12 L146 28 L154 20 L200 20 L210 20 L218 6 L226 30 L234 20 L300 20"
                  fill="none"
                  stroke="oklch(0.85 0.18 150)"
                  strokeWidth="2"
                  strokeLinecap="round"
                  strokeDasharray="600"
                  className="animate-ecg"
                />
              </svg>
            </div>
          </div>

          {/* Controls */}
          <div className="mt-4 flex items-center justify-center gap-3">
            <button
              aria-label="Bật/tắt micro"
              className="flex h-10 w-10 items-center justify-center rounded-full bg-secondary text-secondary-foreground transition hover:scale-110"
            >
              <Mic className="h-4 w-4" />
            </button>
            <button
              aria-label="Bật/tắt camera"
              className="flex h-10 w-10 items-center justify-center rounded-full bg-secondary text-secondary-foreground transition hover:scale-110"
            >
              <Video className="h-4 w-4" />
            </button>
            <button
              aria-label="Kết thúc"
              className="flex h-10 w-10 items-center justify-center rounded-full bg-destructive text-destructive-foreground transition hover:scale-110"
            >
              <PhoneOff className="h-4 w-4" />
            </button>
          </div>

          {/* Stats */}
          <div className="mt-4 grid grid-cols-3 gap-2 border-t border-border pt-4">
            {[
              { v: "72", l: "Nhịp tim", u: "bpm" },
              { v: "120/80", l: "Huyết áp", u: "mmHg" },
              { v: "98%", l: "SpO₂", u: "" },
            ].map((s) => (
              <div key={s.l} className="text-center">
                <div className="flex items-center justify-center gap-1 text-sm font-bold text-foreground">
                  {s.l === "Nhịp tim" && (
                    <HeartPulse className="h-3.5 w-3.5 text-rose-500" />
                  )}
                  {s.v}
                </div>
                <div className="text-[10px] uppercase tracking-wide text-muted-foreground">
                  {s.l} {s.u}
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Floating mini card */}
      <motion.div
        initial={{ opacity: 0, x: -20, y: 20 }}
        animate={{ opacity: 1, x: 0, y: 0 }}
        transition={{ delay: 0.9, duration: 0.6 }}
        className="absolute -left-6 bottom-10 hidden rounded-2xl border border-white/60 bg-white/90 px-4 py-3 backdrop-blur-md sm:block"
        style={{ boxShadow: "var(--shadow-card)" }}
      >
        <div className="flex items-center gap-3">
          <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-primary/10 text-primary">
            <HeartPulse className="h-4 w-4" />
          </div>
          <div>
            <p className="text-xs text-muted-foreground">Hôm nay</p>
            <p className="text-sm font-semibold">3 lịch khám</p>
          </div>
        </div>
      </motion.div>
    </motion.div>
  );
}
