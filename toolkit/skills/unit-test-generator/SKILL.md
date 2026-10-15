---
name: unit-test-generator
description: Generate complete, runnable unit tests for code in any stack - C#/.NET (xUnit, NUnit, MSTest with NSubstitute/Moq), TypeScript/JavaScript (Vitest, Jest), Python (pytest, unittest.mock), Java/Kotlin (JUnit 5, Mockito, MockK, AssertJ), Go (testing, table-driven tests, testify). Use this skill whenever the user shares a class, service, handler, module, or function and wants tests written, asks for unit tests, test coverage, "write tests for this", AAA tests, theory/parametrized/table-driven tests, or mocking with Moq, NSubstitute, jest.mock, vi.mock, pytest-mock, Mockito, or gomock. Always use this skill for unit-test generation; it produces complete test files with edge cases and proper mocking rather than a single example.
category: Testing
---

# Unit Test Generator

Write complete, runnable unit tests for the supplied code - happy paths, edge cases, and failure cases - using Arrange/Act/Assert structure and proper test doubles, in the project's own test framework.

## Input - point it at your code

You don't need pasted snippets. Accept any scope and figure out what to test yourself:

- **A single class/file/module** - generate tests for it.
- **A folder** - generate (or fill gaps in) tests for every public unit under it.
- **A whole project or repo** - find untested public units and prioritize the ones with real logic.
- **A GitHub URL** - clone first, then scan: `git clone --depth 1 <url> /tmp/repo && cd /tmp/repo`

For a folder/project, list the units you'll cover and start with the highest-value ones. Skip trivial DTOs/data classes. Always say which files you generated tests for.

## Step 1 - detect the stack and existing test setup

| Manifest | Stack | Default framework | Default doubles / asserts |
|---|---|---|---|
| `*.csproj` | C# / .NET | xUnit | NSubstitute + FluentAssertions |
| `package.json` | TS / JS | Vitest (Jest if already present) | `vi.fn`/`vi.mock` or `jest.fn`/`jest.mock` |
| `pyproject.toml`, `requirements*.txt` | Python | pytest | `unittest.mock` / `pytest-mock` (`mocker`) |
| `pom.xml`, `build.gradle(.kts)` | Java / Kotlin | JUnit 5 | Mockito + AssertJ (Kotlin: MockK) |
| `go.mod` | Go | `testing` (table-driven) | hand-written fakes on interfaces; `testify` if already used |

**Always match what the project already uses** - read one existing test file for framework, mocking library, assertion style, naming, and file layout (`*Tests.cs`, `*.test.ts`/`*.spec.ts`, `test_*.py`, `*Test.java`, `*_test.go`). Only ask if there are no tests yet and the choice is genuinely ambiguous.

Find targets and existing coverage, e.g.:
```
# .NET
grep -rln "public class\|public record\|public sealed class" --include=*.cs <target> | grep -vi "/obj/\|/bin/\|Tests"
# Node / TS
find <target> -name "*.test.ts" -o -name "*.spec.ts" | grep -v node_modules
# Python
find <target> -name "test_*.py" -o -name "*_test.py"
# Java / Kotlin
find <target> -path "*src/test*" -name "*Test.*"
# Go
find <target> -name "*_test.go"
```

## Workflow

1. **Read the unit under test.** Identify public behavior, dependencies to replace, branches, and edge cases.
2. **Enumerate test cases** before writing: happy path, each branch, boundary values, null/empty/zero inputs, error paths, and every business rule.
3. **Generate the test file** - one behavior per test, descriptive names, AAA layout, doubles only for real dependencies.
4. **Data-drive repeated cases** instead of copy-pasting tests (see mapping table).
5. **Call out gaps** - anything not unit-testable (hits DB/HTTP/filesystem) gets flagged for `integration-test-setup`, not faked.

## Universal conventions

- **Naming reads like a spec:** `Method_Scenario_Expected` (.NET/Java), `it("returns failure when customer has no credit")` (JS), `test_place_order_without_credit_returns_failure` (Python), `TestPlaceOrder/no_credit` subtests (Go).
- **Structure:** explicit Arrange / Act / Assert (comments or blank lines).
- **One behavior per test.** Multiple asserts are fine only when they describe one outcome.
- **No logic in tests** - no loops/conditionals computing expected values; use parametrized data.
- **Mock only what you own / real dependencies** (repositories, clients, clocks). Don't mock value objects, pure functions, or the unit under test.
- **Control time and randomness** - inject a clock (`TimeProvider`, fake timers, `freezegun`/`time-machine`, `Clock`, a `now func()`), seed or inject random sources.
- **Async tests must await** - a test that doesn't await the result can't fail.

## Per-stack mapping

| Concern | C# / .NET | TS / JS | Python | Java / Kotlin | Go |
|---|---|---|---|---|---|
| Test | `[Fact]` | `it` / `test` | `def test_...` | `@Test` | `func TestX(t *testing.T)` |
| Data-driven | `[Theory]` + `[InlineData]`/`[MemberData]` | `it.each` / `test.each` | `@pytest.mark.parametrize` | `@ParameterizedTest` + `@ValueSource`/`@CsvSource`/`@MethodSource` | table-driven + `t.Run` |
| Double | `Substitute.For<T>()` / `new Mock<T>()` | `vi.fn()` / `jest.fn()`, `vi.mock()` | `Mock()`, `AsyncMock()`, `mocker.patch` | `@Mock` / `mock()`, `mockk()` | fake struct implementing the interface |
| Verify call | `Received(1)` / `Verify(...)` | `toHaveBeenCalledWith` | `assert_called_once_with` | `verify(...)` | assert on fake's recorded calls |
| Expect error | `Should().ThrowAsync<T>()` / `Assert.ThrowsAsync` | `await expect(...).rejects.toThrow()` | `with pytest.raises(...)` | `assertThrows` / `assertThatThrownBy` | `if err == nil` / `errors.Is` |
| Setup | constructor / `IAsyncLifetime` | `beforeEach` | fixtures | `@BeforeEach` | helper funcs, `t.Cleanup` |

## C# / .NET

Default **xUnit + NSubstitute + FluentAssertions**; if the project uses **Moq**, **NUnit**, **MSTest**, or built-in `Assert`, match it.

```csharp
public class OrderServiceTests
{
    private readonly IOrderRepository _repo = Substitute.For<IOrderRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly OrderService _sut;

    public OrderServiceTests() => _sut = new OrderService(_repo, _uow);

    [Fact]
    public async Task PlaceOrder_WhenValid_PersistsAndReturnsId()
    {
        // Arrange
        var cmd = new CreateOrderCommand(CustomerId: Guid.NewGuid(), Lines: new() { new("SKU1", 2) });

        // Act
        var result = await _sut.PlaceOrderAsync(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _repo.Received(1).AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PlaceOrder_WithNonPositiveQuantity_ReturnsFailure(int qty)
    {
        var cmd = new CreateOrderCommand(Guid.NewGuid(), new() { new("SKU1", qty) });

        var result = await _sut.PlaceOrderAsync(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _repo.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }
}
```
- Async tests return `Task`, never `async void`.
- Use `TimeProvider` (or an `IClock`) instead of `DateTime.UtcNow` in the unit under test so time is controllable.

## TypeScript / JavaScript

```ts
import { describe, it, expect, vi, beforeEach } from "vitest";

describe("OrderService.placeOrder", () => {
  const repo = { add: vi.fn() };
  let sut: OrderService;
  beforeEach(() => { vi.resetAllMocks(); sut = new OrderService(repo); });

  it("persists a valid order", async () => {
    const result = await sut.placeOrder({ customerId: "c1", lines: [{ sku: "SKU1", qty: 2 }] });
    expect(result.ok).toBe(true);
    expect(repo.add).toHaveBeenCalledTimes(1);
  });

  it.each([0, -1])("rejects quantity %i", async (qty) => {
    const result = await sut.placeOrder({ customerId: "c1", lines: [{ sku: "SKU1", qty }] });
    expect(result.ok).toBe(false);
    expect(repo.add).not.toHaveBeenCalled();
  });
});
```
Prefer constructor-injected fakes over module mocking (`vi.mock`/`jest.mock`); use module mocks only when the code imports a dependency directly. Use `vi.useFakeTimers()` / `jest.useFakeTimers()` for time.

## Python

```python
import pytest
from unittest.mock import AsyncMock

@pytest.fixture
def repo():
    return AsyncMock()

async def test_place_order_valid_persists(repo):
    result = await OrderService(repo).place_order(customer_id="c1", lines=[("SKU1", 2)])
    assert result.ok
    repo.add.assert_awaited_once()

@pytest.mark.parametrize("qty", [0, -1])
async def test_place_order_non_positive_qty_fails(repo, qty):
    result = await OrderService(repo).place_order(customer_id="c1", lines=[("SKU1", qty)])
    assert not result.ok
    repo.add.assert_not_awaited()
```
Async tests need `pytest-asyncio` or `anyio` (match the project's marker/config). Use `autospec=True` / `create_autospec` so mocks fail on wrong signatures.

## Java / Kotlin

JUnit 5 + Mockito (`@ExtendWith(MockitoExtension.class)`, `@Mock`, `@InjectMocks`) + AssertJ; `@ParameterizedTest` with `@ValueSource(ints = {0, -1})` for boundaries. Kotlin: MockK (`mockk`, `coEvery`, `coVerify` for coroutines), `runTest` from kotlinx-coroutines-test. Keep Spring out of unit tests - no `@SpringBootTest` for pure logic.

## Go

```go
func TestPlaceOrder(t *testing.T) {
	tests := []struct {
		name    string
		qty     int
		wantErr bool
	}{
		{"valid", 2, false},
		{"zero qty", 0, true},
		{"negative qty", -1, true},
	}
	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			repo := &fakeRepo{}
			svc := NewOrderService(repo)
			_, err := svc.PlaceOrder(context.Background(), "c1", []Line{{SKU: "SKU1", Qty: tt.qty}})
			if (err != nil) != tt.wantErr {
				t.Fatalf("err = %v, wantErr %v", err, tt.wantErr)
			}
		})
	}
}
```
Prefer small hand-written fakes over generated mocks; use `testify` (`require`/`assert`) or `gomock` only if the project already does. Mark helpers with `t.Helper()`.

## Output format

```
## Test plan
<stack + framework detected; bulleted list of the cases you'll cover and why>

## Tests
<the full test file(s), ready to run>

## Not covered here (needs integration tests)
<anything that hits real I/O - flagged, not mocked into meaninglessness>
```

## Principles

- Tests document behavior - names should read like a spec.
- Cover the branches that matter, not vanity 100% coverage.
- A test that can't fail is worse than no test. Every test must have a way to go red.
- Match the project's existing stack; defaults apply only to greenfield test suites.
