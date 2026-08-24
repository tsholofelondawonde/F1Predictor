import { ImageResponse } from "next/og";

export const size = { width: 1200, height: 630 };
export const contentType = "image/png";

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
        <div style={{ width: "72px", height: "8px", backgroundColor: "#ef4444", marginBottom: "40px" }} />
        <div style={{ fontSize: "108px", fontWeight: 700, letterSpacing: "-2px" }}>GridMind</div>
        <div style={{ marginTop: "24px", fontSize: "36px", color: "#8a8a8a" }}>
          F1 race &amp; championship predictions
        </div>
      </div>
    ),
    { ...size },
  );
}
