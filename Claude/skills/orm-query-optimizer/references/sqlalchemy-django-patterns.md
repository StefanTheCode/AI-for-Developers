# SQLAlchemy / Django ORM - Advanced Patterns Reference

Read this when the basic checklist isn't enough for a Python data layer.

## SQLAlchemy (2.0 style)

### Choose the loader per relationship
```python
from sqlalchemy import select
from sqlalchemy.orm import selectinload, joinedload

stmt = (
    select(Order)
    .options(
        joinedload(Order.customer),      # many-to-one: JOIN is fine
        selectinload(Order.items),       # collection: separate SELECT ... IN, no row explosion
    )
    .where(Order.status == "paid")
    .order_by(Order.id)
    .limit(50)
)
orders = session.scalars(stmt).unique().all()
```
`joinedload` on collections multiplies rows; prefer `selectinload` for one-to-many/many-to-many.

### Make lazy loads fail loudly
```python
stmt = select(Order).options(selectinload(Order.items), raiseload("*"))
```
Or `relationship(..., lazy="raise")`. Any accidental lazy load then raises instead of silently firing N+1. Under `AsyncSession` a lazy load fails anyway (no implicit IO), so eager-load explicitly.

### Projection
```python
rows = session.execute(select(Order.id, Order.total).where(Order.status == "paid")).all()
# or keep entities but load fewer columns:
select(Order).options(load_only(Order.id, Order.total))
```
Column selects skip identity-map and change-tracking work entirely.

### Existence, bulk, streaming
```python
from sqlalchemy import exists, update, insert

found = session.scalar(select(exists().where(User.email == email)))
session.execute(update(Order).where(Order.created_at < cutoff).values(status="expired"))
session.execute(insert(Order), [{"customer_id": 1, "total": 10}, ...])   # executemany
for o in session.scalars(select(Order).execution_options(yield_per=1000)):
    ...
```

### Inspect
`create_engine(url, echo=True)`; `print(stmt.compile(compile_kwargs={"literal_binds": True}))` for debugging only.

## Django ORM

### `select_related` vs `prefetch_related`
```python
# BAD: 1 + N queries (template or serializer touches order.customer and order.items)
orders = Order.objects.filter(status="paid")

# GOOD
orders = (
    Order.objects.filter(status="paid")
    .select_related("customer")                     # FK / one-to-one: SQL JOIN
    .prefetch_related(
        Prefetch("items", queryset=Item.objects.only("id", "name", "order_id"))
    )                                               # reverse FK / M2M: one extra query
    .order_by("id")[:50]
)
```
In DRF, put this in the view's `get_queryset()`; nested serializers are the most common N+1 source.

### Projection
- `.values("id", "total")` / `.values_list("id", flat=True)` - dicts/tuples, no model instances.
- `.only(...)` / `.defer(...)` - fewer columns, still models (touching a deferred field triggers a query per row).

### Existence and counting
- `qs.exists()` instead of `if qs:` or `len(qs) > 0`.
- `qs.count()` instead of `len(qs)` when you don't need the rows.

### Bulk writes and DB-side updates
```python
Order.objects.filter(created_at__lt=cutoff).update(status="expired")
Product.objects.filter(id=pid).update(stock=F("stock") - 1)        # no race, no fetch
Order.objects.bulk_create(objs, batch_size=1000)
Order.objects.bulk_update(objs, ["status"], batch_size=1000)
```
`update()` and `bulk_*` skip `save()` and `pre_save`/`post_save` signals.

### Large result sets
`qs.iterator(chunk_size=2000)` streams without caching the whole queryset.

### Inspect and guard
- `print(qs.query)`, `qs.explain()`, django-debug-toolbar.
- In tests: `with self.assertNumQueries(2): ...` locks in the fix.
- Indexes: `class Meta: indexes = [models.Index(fields=["customer", "-created_at"])]`.
