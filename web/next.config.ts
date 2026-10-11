import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Required by Aspire's AddNextJsApp for publish mode (container image built in Step 10).
  output: "standalone",
  // Stop `next dev` from writing AGENTS.md / CLAUDE.md; repo-wide agent rules live in .github/.
  agentRules: false,
};

export default nextConfig;
