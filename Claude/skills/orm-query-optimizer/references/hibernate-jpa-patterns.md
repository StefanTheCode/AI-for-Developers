# Hibernate / JPA / Spring Data - Advanced Patterns Reference

Read this when the basic checklist isn't enough for a Java/Kotlin data layer.

## Fetch types: fix the defaults

`@ManyToOne` and `@OneToOne` are **EAGER by default** in JPA. Every load of the owning entity drags them along, often as extra queries.
```java
@ManyToOne(fetch = FetchType.LAZY)
private Customer customer;
```
Make everything LAZY, then fetch explicitly per use case.

## Fetch explicitly per query
```java
// JPQL
@Query("select o from Order o join fetch o.customer where o.status = :status")
List<Order> findPaidWithCustomer(@Param("status") Status status);

// Spring Data entity graph
@EntityGraph(attributePaths = {"customer", "items"})
List<Order> findByStatus(Status status);
```

### Pitfalls
- **`MultipleBagFetchException`:** fetching two `List` collections in one query. Fetch one collection per query, use `Set` where semantics allow, or use batch fetching.
- **Collection fetch + pagination:** `join fetch` of a collection with `Pageable` makes Hibernate paginate **in memory** (it logs a warning). Page the IDs first, then fetch the graph `where o.id in :ids`.

## Batch fetching (cheap N+1 mitigation)
```properties
spring.jpa.properties.hibernate.default_batch_fetch_size=50
```
Or `@BatchSize(size = 50)` on a collection. Lazy loads then run as `WHERE id IN (...)` batches instead of one per row.

## DTO projections
```java
public record OrderSummary(Long id, BigDecimal total, String customerName) {}

@Query("select new com.acme.orders.OrderSummary(o.id, o.total, c.name) " +
       "from Order o join o.customer c where o.status = :status")
List<OrderSummary> findSummaries(@Param("status") Status status);
```
Spring Data also supports interface-based and class-based (record) projections as repository return types. Projections skip the persistence context entirely.

## Read-only work
- `@Transactional(readOnly = true)` on query services - Hibernate can skip dirty checking for that session.
- Per query: hint `org.hibernate.readOnly` = `true`.
- Set `spring.jpa.open-in-view=false`. Open Session in View hides N+1 queries fired during JSON serialization.

## Pagination and counting
- `Page<T>` runs an extra `count` query; use `Slice<T>` when you only need "has next".
- Keyset: `findByIdGreaterThanOrderByIdAsc(Long lastId, Limit limit)` or `Pageable.ofSize(n)` with an `id > :last` condition.
- Existence: `existsByEmail(String email)` instead of `findByEmail(...) != null`.

## Writes
```properties
spring.jpa.properties.hibernate.jdbc.batch_size=50
spring.jpa.properties.hibernate.order_inserts=true
spring.jpa.properties.hibernate.order_updates=true
```
- `GenerationType.IDENTITY` disables JDBC insert batching; use `SEQUENCE` (with an allocation size) for bulk inserts.
- Bulk update/delete in one statement:
```java
@Modifying(clearAutomatically = true)
@Query("update Order o set o.status = :s where o.createdAt < :cutoff")
int expireOlderThan(@Param("s") Status s, @Param("cutoff") Instant cutoff);
```
Bulk JPQL bypasses the persistence context and entity listeners.
- For large batch jobs, `flush()` and `clear()` the `EntityManager` every N rows.

## Inspect
- `spring.jpa.show-sql=true` (dev) or logger `org.hibernate.SQL=DEBUG`.
- `hibernate.generate_statistics=true` reports query counts per session - a quick N+1 detector.
- Indexes belong in Flyway/Liquibase migrations; `@Table(indexes = @Index(columnList = "customer_id"))` only documents them for schema generation.
