---
description: Runs full pipeline: Planner (Flash) → Plan Review (Pro) → Coder (Flash) → Code Review (Pro)
agent: planner
model: deepseek:v4-flash
---

$user_request

Go through the full pipeline strictly following this sequence:

1. Create a detailed implementation plan as Planner.
2. Ask the plan reviewer: "@plan-reviewer review this plan thoroughly"
3. Wait for the plan review to complete.
4. Once the plan is approved or improved — ask the coder: "@coder implement the plan"
5. After the coder finishes — ask the code reviewer: "@code-reviewer perform a full code review"
6. At the end, give a short summary of the whole process.

Work sequentially and carefully. Do not skip any steps.