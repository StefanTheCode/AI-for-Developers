# Prisma / TypeORM / Drizzle - Advanced Patterns Reference

Read this when the basic checklist isn't enough for a Node/TypeScript data layer.

## Prisma

### Eager loading vs projection
```ts
// BAD: N+1 - one query per user
const users = await prisma.user.findMany();
for (const u of users) {
  u.orders = await prisma.order.findMany({ where: { userId: u.id } });
}

// GOOD: project only what the caller needs (one round of queries, no over-fetching)
const users = await prisma.user.findMany({
  select: {
    id: true,
    name: true,
    orders: { select: { id: true, total: true }, orderBy: { createdAt: 'desc' }, take: 5 },
  },
});
```
`select` and `include` can't be combined at the same level - prefer `select` for read endpoints. Prisma can load relations with separate queries or a single JOIN (`relationLoadStrategy`); check the docs for your version before relying on it.

### Cursor pagination
```ts
const page = await prisma.order.findMany({
  take: 50,
  skip: lastId ? 1 : 0,               // skip the cursor row itself
  ...(lastId && { cursor: { id: lastId } }),
  orderBy: { id: 'asc' },
});
```

### Existence and counting
```ts
const exists = (await prisma.user.findFirst({ where: { email }, select: { id: true } })) !== null;
const n = await prisma.order.count({ where: { status: 'PENDING' } });
```

### Bulk writes
`createMany`, `updateMany`, `deleteMany` run one statement. Wrap related writes in `prisma.$transaction([...])`. Don't loop `update()` per row.

### Raw SQL
``prisma.$queryRaw`SELECT ... WHERE id = ${id}` `` is parameterized (tagged template). `$queryRawUnsafe` with string concatenation is an injection risk.

### Indexes and SQL logging
- Schema: `@@index([customerId, createdAt])`, then `prisma migrate dev`.
- Logging: `new PrismaClient({ log: ['query'] })`.

## TypeORM

### Lazy relations are N+1 in disguise
Relations typed as `Promise<T>` load on access. Prefer explicit loading:
```ts
const orders = await orderRepo.find({
  select: { id: true, total: true, customer: { id: true, name: true } },
  relations: { customer: true },
  where: { status: 'PAID' },
  order: { id: 'ASC' },
  take: 50,
});
```
For several collections, `relationLoadStrategy: 'query'` loads relations in separate queries instead of one exploding JOIN.

### QueryBuilder projection
```ts
const rows = await dataSource.getRepository(Order)
  .createQueryBuilder('o')
  .select(['o.id', 'o.total'])
  .leftJoin('o.customer', 'c')
  .addSelect(['c.name'])
  .where('o.status = :status', { status: 'PAID' })
  .getRawMany();           // plain rows, no entity hydration
```
With joins, paginate with `.take()/.skip()`, not `.limit()/.offset()`.

### Writes
- `repo.save(array)` selects each entity first to decide insert vs update; for pure inserts use `repo.insert(array)`.
- Bulk update: `createQueryBuilder().update(Order).set({ status: 'EXPIRED' }).where('createdAt < :c', { c }).execute()`.

### Logging
`new DataSource({ ..., logging: ['query'] })` or `logging: true`; `.getSql()` on a QueryBuilder.

## Drizzle

### Relational queries compile to one SQL statement
```ts
const users = await db.query.users.findMany({
  columns: { id: true, name: true },
  with: { posts: { columns: { id: true, title: true }, limit: 5 } },
});
```

### Core API projection + batching
```ts
import { eq, inArray, gt } from 'drizzle-orm';

const rows = await db
  .select({ id: orders.id, total: orders.total })
  .from(orders)
  .where(inArray(orders.customerId, customerIds))   // batch instead of N queries
  .orderBy(orders.id)
  .limit(50);
```
Keyset page: `.where(gt(orders.id, lastId)).orderBy(orders.id).limit(50)`.

### Writes, indexes, logging
- Bulk insert: `db.insert(orders).values(rows)`; update: `db.update(orders).set({...}).where(...)`.
- Index in schema: `(t) => [index('orders_customer_idx').on(t.customerId)]`.
- Logging: `drizzle(client, { logger: true })`; `.toSQL()` on a query.
