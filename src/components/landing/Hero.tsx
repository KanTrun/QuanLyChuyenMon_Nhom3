import { useEffect, useState } from "react";
import { motion, useMotionValue, useTransform, animate } from "framer-motion";
import {
  ArrowRight,
  Menu,
  Search,
  Shield,
  Star,
  Stethoscope,
  X,
} from "lucide-react";
import { ConsultCard } from "./ConsultCard";

const navLinks = [
  { label: "Tư vấn video", href: "#tu-van" },
  { label: "Chuyên khoa", href: "#chuyen-khoa" },
  { label: "Theo dõi sức khỏe", href: "#theo-doi" },
];

function Counter({ to, suffix = "" }: { to: number; suffix?: string }) {
  const count = useMotionValue(0);
  const rounded = useTransform(count, (v) => Math.round(v).toLocaleString("vi-VN"));
  useEffect(() => {
    const c = animate(count, to, { duration: 2, ease: "easeOut" });
    return c.stop;
  }, [to, count]);
  return (
    <span className="font-bold text-foreground">
      <motion.span>{rounded}</motion.span>
      {suffix}
    </span>
  );
}

function Header() {
  const [scrolled, setScrolled] = useState(false);
  const [open, setOpen] = useState(false);

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 12);
    onScroll();
    window.addEventListener("scroll", onScroll);
    return () => window.removeEventListener("scroll", onScroll);
  }, []);

  return (
    <header
      className={`sticky top-0 z-50 transition-all duration-300 ${
        scrolled
          ? "border-b border-border bg-background/80 backdrop-blur-xl"
          : "bg-transparent"
      }`}
    >
      <nav
        aria-label="Điều hướng chính"
        className="mx-auto flex max-w-7xl items-center justify-between px-6 py-4"
      >
        <a href="/" className="flex items-center gap-2.5">
          <div
            className="flex h-9 w-9 items-center justify-center rounded-xl text-white shadow-lg"
            style={{ background: "var(--gradient-hero)" }}
          >
            <Stethoscope className="h-5 w-5" />
          </div>
          <span className="text-lg font-bold tracking-tight text-foreground">
            Bệnh viện<span className="text-primary"> số</span>
          </span>
        </a>

        <ul className="hidden items-center gap-8 md:flex">
          {navLinks.map((l) => (
            <li key={l.href}>
              <a
                href={l.href}
                className="group relative text-sm font-medium text-muted-foreground transition-colors hover:text-foreground"
              >
                {l.label}
                <span className="absolute -bottom-1 left-0 h-0.5 w-0 bg-primary transition-all duration-300 group-hover:w-full" />
              </a>
            </li>
          ))}
        </ul>

        <div className="hidden items-center gap-3 md:flex">
          <a
            href="#dang-nhap"
            className="text-sm font-medium text-foreground transition hover:text-primary"
          >
            Đăng nhập
          </a>
          <a
            href="#dang-ky"
            className="group inline-flex items-center gap-1.5 rounded-full px-4 py-2 text-sm font-semibold text-primary-foreground transition-all hover:scale-[1.03]"
            style={{
              background: "var(--gradient-hero)",
              boxShadow: "var(--shadow-glow)",
            }}
          >
            Đăng ký ngay
            <ArrowRight className="h-3.5 w-3.5 transition-transform group-hover:translate-x-0.5" />
          </a>
        </div>

        <button
          aria-label="Mở menu"
          onClick={() => setOpen((v) => !v)}
          className="flex h-10 w-10 items-center justify-center rounded-lg border border-border bg-card md:hidden"
        >
          {open ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
        </button>
      </nav>

      {open && (
        <motion.div
          initial={{ opacity: 0, height: 0 }}
          animate={{ opacity: 1, height: "auto" }}
          exit={{ opacity: 0, height: 0 }}
          className="border-t border-border bg-background/95 backdrop-blur md:hidden"
        >
          <div className="flex flex-col gap-4 p-6">
            {navLinks.map((l) => (
              <a
                key={l.href}
                href={l.href}
                className="text-base font-medium text-foreground"
              >
                {l.label}
              </a>
            ))}
            <a href="#dang-nhap" className="text-base font-medium text-foreground">
              Đăng nhập
            </a>
            <a
              href="#dang-ky"
              className="inline-flex w-fit items-center gap-2 rounded-full px-5 py-2.5 text-sm font-semibold text-primary-foreground"
              style={{ background: "var(--gradient-hero)" }}
            >
              Đăng ký ngay <ArrowRight className="h-4 w-4" />
            </a>
          </div>
        </motion.div>
      )}
    </header>
  );
}

function Background() {
  return (
    <div aria-hidden className="pointer-events-none absolute inset-0 -z-10 overflow-hidden">
      <div className="absolute inset-0" style={{ background: "var(--gradient-bg)" }} />
      {/* Grid */}
      <div
        className="absolute inset-0 opacity-[0.04]"
        style={{
          backgroundImage:
            "linear-gradient(currentColor 1px, transparent 1px), linear-gradient(90deg, currentColor 1px, transparent 1px)",
          backgroundSize: "56px 56px",
          color: "oklch(0.3 0.1 230)",
          maskImage:
            "radial-gradient(ellipse at center, black 40%, transparent 75%)",
        }}
      />
      {/* Blobs */}
      <div
        className="animate-blob absolute -left-32 top-10 h-96 w-96 rounded-full opacity-40 blur-3xl"
        style={{ background: "var(--primary-glow)" }}
      />
      <div
        className="animate-blob-slow absolute -right-32 top-40 h-[28rem] w-[28rem] rounded-full opacity-30 blur-3xl"
        style={{ background: "var(--primary)" }}
      />
    </div>
  );
}

const fadeUp = {
  hidden: { opacity: 0, y: 24 },
  show: (i: number) => ({
    opacity: 1,
    y: 0,
    transition: { delay: i * 0.08, duration: 0.7, ease: [0.22, 1, 0.36, 1] as const },
  }),
};

export function Hero() {
  return (
    <div className="relative min-h-screen">
      <Background />
      <Header />

      <section className="relative mx-auto grid max-w-7xl grid-cols-1 items-center gap-14 px-6 py-16 lg:grid-cols-12 lg:gap-10 lg:py-24">
        {/* Left: copy */}
        <div className="lg:col-span-7">
          <motion.div
            custom={0}
            initial="hidden"
            animate="show"
            variants={fadeUp}
            className="inline-flex items-center gap-2 rounded-full border border-primary/20 bg-primary/5 px-3 py-1.5 text-xs font-medium text-primary"
          >
            <span className="relative flex h-2 w-2">
              <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-primary/60" />
              <span className="relative inline-flex h-2 w-2 rounded-full bg-primary" />
            </span>
            Trực tuyến · 240 bác sĩ sẵn sàng tư vấn
          </motion.div>

          <motion.h1
            custom={1}
            initial="hidden"
            animate="show"
            variants={fadeUp}
            className="mt-6 text-balance text-4xl font-bold leading-[1.05] tracking-tight text-foreground sm:text-5xl lg:text-6xl"
          >
            Nền tảng y tế từ xa{" "}
            <span
              className="bg-clip-text text-transparent"
              style={{ backgroundImage: "var(--gradient-text)" }}
            >
              thế hệ mới
            </span>
          </motion.h1>

          <motion.p
            custom={2}
            initial="hidden"
            animate="show"
            variants={fadeUp}
            className="mt-4 text-xl font-medium text-foreground/80 sm:text-2xl"
          >
            Sức khỏe của bạn, được chăm sóc{" "}
            <span className="italic text-primary">mọi lúc mọi nơi.</span>
          </motion.p>

          <motion.p
            custom={3}
            initial="hidden"
            animate="show"
            variants={fadeUp}
            className="mt-5 max-w-xl text-base leading-relaxed text-muted-foreground"
          >
            Tiếp cận các chuyên gia y tế hàng đầu qua các phiên khám video bảo mật
            cao. Quản lý hồ sơ sức khỏe và theo dõi chỉ số sống một cách dễ dàng
            với giao diện thân thiện, hiện đại.
          </motion.p>

          <motion.div
            custom={4}
            initial="hidden"
            animate="show"
            variants={fadeUp}
            className="mt-8 flex flex-wrap items-center gap-3"
          >
            <a
              href="#dat-lich"
              className="group relative inline-flex items-center gap-2 overflow-hidden rounded-full px-7 py-3.5 text-sm font-semibold text-primary-foreground transition-transform hover:scale-[1.03]"
              style={{
                background: "var(--gradient-hero)",
                boxShadow: "var(--shadow-glow)",
              }}
            >
              <span className="relative z-10">Đặt lịch khám ngay</span>
              <ArrowRight className="relative z-10 h-4 w-4 transition-transform group-hover:translate-x-1" />
              <span className="absolute inset-y-0 -left-10 w-10 bg-white/30 opacity-0 group-hover:animate-shine group-hover:opacity-100" />
            </a>
            <a
              href="#tim-bac-si"
              className="inline-flex items-center gap-2 rounded-full border border-border bg-card px-6 py-3.5 text-sm font-semibold text-foreground transition hover:border-primary/40 hover:bg-primary/5"
            >
              <Search className="h-4 w-4" />
              Tìm bác sĩ
            </a>
          </motion.div>

          {/* Trust row */}
          <motion.div
            custom={5}
            initial="hidden"
            animate="show"
            variants={fadeUp}
            className="mt-10 flex flex-wrap items-center gap-5"
          >
            <div className="flex -space-x-2">
              {[
                "from-rose-300 to-rose-500",
                "from-amber-300 to-orange-500",
                "from-emerald-300 to-teal-500",
                "from-sky-300 to-indigo-500",
              ].map((g) => (
                <div
                  key={g}
                  className={`h-9 w-9 rounded-full bg-gradient-to-br ring-2 ring-background ${g}`}
                  aria-hidden
                />
              ))}
              <div className="flex h-9 w-9 items-center justify-center rounded-full bg-primary text-[10px] font-bold text-primary-foreground ring-2 ring-background">
                +9k
              </div>
            </div>
            <div>
              <div className="flex items-center gap-1 text-amber-500">
                {[...Array(5)].map((_, i) => (
                  <Star key={i} className="h-4 w-4 fill-current" />
                ))}
                <span className="ml-1 text-xs font-semibold text-foreground">
                  4.9/5
                </span>
              </div>
              <p className="mt-0.5 text-xs text-muted-foreground">
                Được tin dùng bởi hơn <Counter to={10000} suffix="+" /> bệnh nhân
              </p>
            </div>
            <div className="flex items-center gap-2 rounded-full border border-border bg-card/60 px-3 py-1.5 text-xs text-muted-foreground backdrop-blur">
              <Shield className="h-3.5 w-3.5 text-emerald-600" />
              Bảo mật chuẩn HIPAA
            </div>
          </motion.div>
        </div>

        {/* Right: card */}
        <div className="lg:col-span-5">
          <ConsultCard />
        </div>
      </section>
    </div>
  );
}
