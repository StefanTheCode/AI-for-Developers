# How to use - Test Data Builder Generator

**What it is.** Generates test data builders, factories, and object mothers so tests create valid domain objects in one line, with only the relevant fields made explicit. It detects your stack and uses its idiom: fluent builders with Bogus/AutoFixture (.NET), fishery + @faker-js/faker (TS/JS), factory_boy/Faker/polyfactory (Python), builders with Instancio/Datafaker or Kotlin default arguments (JVM), builders/functional options with gofakeit (Go).

**When to reach for it.** Repetitive, brittle object setup across tests, or objects with invariants that are painful to construct.

**How to use it.** Point it at the types and their tests (path or GitHub URL). Example prompts:
- "Generate a test data builder and object mother for `Order`."
- "Create fishery factories for the types in `src/domain/`."
- "factory_boy factories for my Django models, with a `confirmed` trait."
- "Builders vs Instancio for these Java entities?"
- "Reduce the setup boilerplate in `orders_test.go`."

**Get the best out of it.** Let it read existing tests to see the real setup pain. Ask it to build through your constructors/factories so invariants hold. Ask for seeded fakers if you need reproducible failures.

**What you get.** The detected approach and library recommendation, the generated builders/factories with named scenarios, one existing test rewritten as a before/after, and notes on seeding and persistence helpers.

**Won't do.** It won't bypass domain invariants to fabricate invalid objects by accident. Pairs with `unit-test-generator` and `integration-test-setup`.
