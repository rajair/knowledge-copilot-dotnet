import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Knowledge Copilot",
  description: "Permission-aware answers from your internal knowledge.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
