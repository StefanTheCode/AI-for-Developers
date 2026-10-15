# How to use - Unit Test Generator

**What it is.** A skill that writes complete, runnable unit tests for your code in any stack - happy paths, edge cases, and failure cases - with Arrange/Act/Assert structure, descriptive names, and proper test doubles. It detects the stack and matches your existing setup: xUnit/NUnit with NSubstitute or Moq (.NET), Vitest or Jest (TS/JS), pytest (Python), JUnit 5 with Mockito/MockK (Java/Kotlin), table-driven `testing` (Go).

**When to reach for it.** New code that needs tests, raising coverage on a service/handler/module, or generating a test scaffold you'll refine.

**How to use it.** Point it at what you want tested - a **file**, a **folder**, a whole **project**, or a **GitHub URL**. For a folder or project it finds untested public units and prioritizes the ones with real logic. Example prompts:
- "Write xUnit tests for `OrderService.cs`."
- "Generate Vitest tests for everything in `src/orders/`."
- "Write pytest tests for `billing/invoice.py`, including the error paths."
- "Add JUnit 5 + Mockito tests for `PaymentService`."
- "Table-driven tests for `pricing.go`."
- "Find untested code in this repo and write tests for the important parts: https://github.com/me/myapi"

**How to get the best out of it.**
- **Give it the dependencies' interfaces/types** so it replaces the right seams instead of guessing.
- **Ask for the test plan first** on a complex unit - it lists the cases so you can add any it missed before it writes them.
- **Point out the business rules** that matter; it turns them into named tests that read like a spec.
- **Ask it to consolidate** repetitive cases into data-driven tests (`[Theory]`, `it.each`, `parametrize`, `@ParameterizedTest`, table-driven).

**What you get.** A test plan, the full test file(s) in your framework and style, and a list of what needs integration tests instead.

**What it won't do.** It won't write meaningful tests for code that needs real I/O - it flags those for `integration-test-setup` instead of mocking them into nonsense. It won't chase vanity 100% coverage; it targets the branches that matter.
