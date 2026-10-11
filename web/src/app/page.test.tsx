import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";

import Home from "./page";

afterEach(cleanup);

describe("Home", () => {
  it("renders the product name as the main heading", () => {
    render(<Home />);

    expect(
      screen.getByRole("heading", { level: 1, name: "Knowledge Copilot" }),
    ).toBeDefined();
  });
});
