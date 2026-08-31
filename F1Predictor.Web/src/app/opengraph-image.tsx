import { ImageResponse } from "next/og";

export const size = { width: 1200, height: 630 };
export const contentType = "image/png";

const mark = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32">
  <g fill="#eaeaea" opacity="0.2">
    <circle cx="6" cy="6" r="0.9"/><circle cx="6" cy="13" r="0.9"/><circle cx="6" cy="20" r="0.9"/><circle cx="6" cy="26" r="0.9"/>
    <circle cx="13" cy="6" r="0.9"/><circle cx="13" cy="13" r="0.9"/><circle cx="13" cy="20" r="0.9"/><circle cx="13" cy="26" r="0.9"/>
    <circle cx="20" cy="6" r="0.9"/><circle cx="20" cy="13" r="0.9"/><circle cx="20" cy="20" r="0.9"/><circle cx="20" cy="26" r="0.9"/>
    <circle cx="26" cy="6" r="0.9"/><circle cx="26" cy="13" r="0.9"/><circle cx="26" cy="20" r="0.9"/><circle cx="26" cy="26" r="0.9"/>
  </g>
  <path d="M4 27C12 26 16 21 20 13S25 6 28 5" fill="none" stroke="#eaeaea" stroke-width="2.6" stroke-linecap="round"/>
  <circle cx="4" cy="27" r="1.8" fill="#eaeaea"/>
  <circle cx="28" cy="5" r="3" fill="#ef4444"/>
</svg>`;

export default function OpengraphImage() {
  return new ImageResponse(
    (
      <div
        style={{
          width: "100%",
          height: "100%",
          display: "flex",
          flexDirection: "column",
          alignItems: "flex-start",
          justifyContent: "center",
          padding: "80px",
          backgroundColor: "#0a0a0a",
          color: "#eaeaea",
          fontFamily: "sans-serif",
        }}
      >
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img
          width={96}
          height={96}
          src={`data:image/svg+xml;utf8,${encodeURIComponent(mark)}`}
          alt=""
          style={{ marginBottom: "36px" }}
        />
        <div style={{ fontSize: "108px", fontWeight: 700, letterSpacing: "-2px" }}>GridMind</div>
        <div style={{ marginTop: "24px", fontSize: "36px", color: "#8a8a8a" }}>
          F1 race &amp; championship predictions
        </div>
      </div>
    ),
    { ...size },
  );
}
