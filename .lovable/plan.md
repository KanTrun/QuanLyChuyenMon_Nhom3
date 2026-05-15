# Hero Section "Bệnh viện số" — Plan

Xây dựng một Hero section chuyên nghiệp, hiện đại cho nền tảng y tế từ xa, thay thế placeholder hiện tại ở `src/routes/index.tsx`. Toàn bộ nội dung tiếng Việt, có chuyển động tinh tế và tương tác.

## Phạm vi

- Chỉ frontend / presentation. Không backend, không auth thật, không gọi API.
- Một file route `index.tsx` + một component `Hero.tsx` tách riêng cho gọn.
- Sử dụng design tokens trong `src/styles.css` (mở rộng palette y tế: xanh trust + xanh mint accent).
- Animation bằng `framer-motion` (cài mới) + Tailwind transitions.

## Cấu trúc giao diện

```text
┌─────────────────────────────────────────────────────────┐
│ NAV: Logo "Bệnh viện số" | Tư vấn video · Chuyên khoa   │
│       · Theo dõi sức khỏe        [Đăng nhập] [Đăng ký]  │
├─────────────────────────────────────────────────────────┤
│ HERO  (2 cột, lệch nhịp)                                │
│  ┌───────────────────────┐  ┌────────────────────────┐  │
│  │ Badge: ● Trực tuyến   │  │  Card "Phòng khám"     │  │
│  │ H1 lớn, gradient      │  │  ┌──────┐  BS Trần... │  │
│  │ Sub-headline          │  │  │avatar│  Đang kết nối│  │
│  │ Mô tả ngắn            │  │  └──────┘  ●●● dots    │  │
│  │ [Đặt lịch] [Tìm BS]   │  │  Waveform / pulse line │  │
│  │ ★★★★★ 10,000+ BN      │  │  3 stat chips ở đáy    │  │
│  └───────────────────────┘  └────────────────────────┘  │
│  Background: gradient mesh + blob blur + grid mờ        │
└─────────────────────────────────────────────────────────┘
```

## Tính năng & tương tác

1. **Header sticky** với hiệu ứng blur khi scroll, underline animation cho nav links.
2. **Headline reveal**: từng dòng fade + slide-up theo stagger khi mount.
3. **Gradient text** cho cụm "thế hệ mới" / "mọi lúc mọi nơi".
4. **CTA chính** "Đặt lịch khám ngay": gradient + glow shadow, hover scale + shine sweep.
5. **CTA phụ** "Tìm bác sĩ": viền, hover fill.
6. **Trust row**: 5 sao + avatar stack chồng + counter "10,000+" đếm từ 0 lên (animate khi vào view).
7. **Card "Phòng khám trực tuyến"** (bên phải):
   - Mock video tile với avatar bác sĩ, đồng hồ live "09:40 AM" cập nhật theo giây thật.
   - Indicator "Đang kết nối" với 3 dot nhảy.
   - Pulse ring quanh avatar (nhịp tim).
   - Mini waveform SVG chạy loop (giả sóng âm/ECG).
   - Float nhẹ lên xuống (y-axis ±8px, ease-in-out).
   - Hover → tilt 3D nhẹ theo vị trí chuột (parallax).
8. **Stat chips** dưới card: 3 mục (VD: "24/7 hỗ trợ", "500+ Bác sĩ", "98% Hài lòng") với icon.
9. **Background**:
   - Gradient mesh xanh ngọc → trắng.
   - 2 blob blur lớn animate trôi chậm.
   - Grid pattern mờ (SVG).
   - Pulse rings nhẹ phía sau card.
10. **Responsive**: mobile xếp dọc, card đẩy xuống dưới, nav thu gọn thành menu icon (chỉ visual, mở/đóng được).
11. **Reduced-motion**: tôn trọng `prefers-reduced-motion` — tắt float/parallax.

## Design tokens (thêm vào `src/styles.css`)

- `--primary`: xanh y tế đậm (oklch ~0.45 0.13 220)
- `--primary-glow`: xanh mint sáng (oklch ~0.85 0.12 180)
- `--accent`: xanh ngọc
- `--gradient-hero`: linear-gradient primary → primary-glow
- `--gradient-text`: cho headline highlight
- `--shadow-glow`: glow xanh quanh CTA & card
- `--shadow-card`: soft elevation cho card phòng khám

## Chi tiết kỹ thuật

- **Files**:
  - `src/routes/index.tsx` — route, head SEO (title, description, og), render `<Hero />`.
  - `src/components/landing/Hero.tsx` — toàn bộ markup hero + nav.
  - `src/components/landing/ConsultCard.tsx` — card phòng khám (tách cho gọn).
  - `src/styles.css` — thêm tokens + keyframes phụ (`pulse-ring`, `shine`, `float`).
- **Dependencies**: `bun add framer-motion`.
- **Icons**: dùng `lucide-react` (đã có) — Stethoscope, Video, HeartPulse, Shield, Star, ArrowRight, Menu.
- **SEO**: title "Bệnh viện số — Khám bệnh từ xa với bác sĩ chuyên khoa", meta description tiếng Việt <160 ký tự, H1 duy nhất, alt tiếng Việt cho avatar.
- **A11y**: nav landmark, button có `aria-label`, focus ring rõ, contrast AA.

## Các bước thực hiện

1. Cài `framer-motion`.
2. Cập nhật `src/styles.css` (tokens y tế + keyframes).
3. Tạo `src/components/landing/ConsultCard.tsx`.
4. Tạo `src/components/landing/Hero.tsx` (nav + hero + background).
5. Thay `src/routes/index.tsx` bằng route render Hero + head SEO.
6. Verify build, kiểm tra preview, tinh chỉnh khoảng cách / animation timing.

## Ngoài phạm vi (có thể làm sau)

- Các section khác (Chuyên khoa, Cách hoạt động, Bác sĩ nổi bật, Footer).
- Trang `/dang-nhap`, `/dat-lich`, `/bac-si` thật.
- Tích hợp Lovable Cloud cho auth & đặt lịch.
