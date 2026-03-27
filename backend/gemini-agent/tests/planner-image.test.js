import test from "node:test";
import assert from "node:assert/strict";
import { createBrowserActionPlanner } from "../src/browserActionPlanner.js";
import { extendedAlternativesForType } from "../src/contentClassifier.js";
import { describeDominantColorsFromImage } from "../src/imageAnalysis.js";

const RED_PIXEL_PNG_BASE64 =
  "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAANSURBVBhXY/jPwPAfAAUAAf+mXJtdAAAAAElFTkSuQmCC";

test("image extended alternatives avoid document-only defaults", () => {
  const actions = extendedAlternativesForType("image");

  assert.ok(actions.includes("extract_dominant_colors"));
  assert.doesNotMatch(actions.join(","), /ocr_extract_text/);
  assert.doesNotMatch(actions.join(","), /extract_table_data/);
});

test("dominant color extraction uses deterministic local image analysis", () => {
  const description = describeDominantColorsFromImage(RED_PIXEL_PNG_BASE64, "image/png");

  assert.match(description, /#FF0000/i);
  assert.match(description, /red/i);
});

test("browser planner rejects numeric quiz hallucinations and falls back to visible answer-key text", async () => {
  const planner = createBrowserActionPlanner({
    generateText: async () => ({
      text: JSON.stringify({
        goal: "answer_current_quiz",
        summary: "Answer the current quiz question by selecting '12'.",
        requiresConfirmation: false,
        steps: [
          {
            tool: "check_radio",
            option: "12"
          }
        ]
      }),
      model: "fake-test-model",
      latencyMs: 5,
      usage: { inputTokens: 10, outputTokens: 10 }
    })
  });

  const plan = await planner({
    originalText: `The day before yesterday, Suzie was 17. Next year, she will be 19. What day is her birthday?
A) February 29
B) January 1
C) April 23
D) December 31`,
    resultText: "Q1 [Suzie's birthday]: February 29 - It only fits on a leap-year timeline.",
    action: "answer_question",
    voiceCommand: "attempt all questions",
    contentType: "mcq",
    browserContext: {
      url: "https://example.com/quiz",
      title: "IQ test quiz",
      visibleText: "2 / 11 February 29 January 1 April 23 December 31 Next",
      interactiveElements: []
    }
  });

  assert.equal(plan.goal, "fill_form_answers");
  const answerKeyStep = plan.steps.find((step) => step.tool === "apply_answer_key");
  assert.ok(answerKeyStep);
  assert.equal(answerKeyStep.advancePages, true);
  assert.ok(answerKeyStep.answers.some((answer) => answer.option === "February 29"));
});

test("mail reply planner uses the visible reply composer without refilling recipient or subject", async () => {
  const planner = createBrowserActionPlanner({
    generateText: async () => {
      throw new Error("Mail reply with visible composer should not call model planner.");
    }
  });

  const plan = await planner({
    originalText: "Hi, can you please confirm tomorrow's session?",
    resultText: "Dear Hewen,\n\nThank you for the reminder. I'll join tomorrow.\n\nBest,\nTanush",
    action: "draft_reply",
    voiceCommand: "reply to this email",
    contentType: "email",
    browserContext: {
      url: "https://mail.google.com/mail/u/0/#inbox/FMfcgzQ",
      title: "Inbox - Gmail",
      visibleText: "Reply Send Dear Hewen Best regards",
      interactiveElements: [
        { role: "textbox", label: "Write a reply", nameAttribute: "", type: "textbox", options: [] },
        { role: "button", label: "Send", nameAttribute: "", type: "button", options: [] }
      ]
    }
  });

  assert.equal(plan.goal, "reply_to_email");
  assert.ok(plan.steps.some((step) => step.tool === "fill_editor" && step.label === "Write a reply"));
  assert.ok(!plan.steps.some((step) => step.tool === "fill_label" && /to recipients|subject/i.test(step.label || "")));
  assert.ok(!plan.steps.some((step) => step.tool === "click_role" && step.name === "Reply"));
});

test("shopping planner opens amazon and flipkart comparison tabs for product-like image results", async () => {
  const planner = createBrowserActionPlanner({
    generateText: async () => {
      throw new Error("Shopping task pack should use direct comparison plan.");
    }
  });

  const plan = await planner({
    originalText: "",
    resultText: "A black silicone iPhone 16 cover with raised camera protection.",
    action: "describe_image",
    voiceCommand: "",
    contentType: "image",
    browserContext: {
      url: "https://example.com/gallery",
      title: "Product screenshot",
      visibleText: "Compare product details and price",
      interactiveElements: []
    }
  });

  assert.equal(plan.goal, "compare_product_prices");
  const openTabs = plan.steps.filter((step) => step.tool === "open_new_tab");
  assert.equal(openTabs.length, 2);
  assert.match(openTabs[0].url, /amazon\.in/);
  assert.match(openTabs[1].url, /flipkart\.com/);
});
