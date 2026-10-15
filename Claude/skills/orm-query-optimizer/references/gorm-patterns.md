# GORM - Advanced Patterns Reference

Read this when the basic checklist isn't enough for a Go service using GORM v2 (`gorm.io/gorm`).

## Preload vs Joins

```go
// BAD: N+1
var users []User
db.Find(&users)
for i := range users {
    db.Where("user_id = ?", users[i].ID).Find(&users[i].Orders)
}

// GOOD: Preload runs one extra query per association (WHERE user_id IN (...))
db.Preload("Orders", "status = ?", "paid").Find(&users)

// Nested
db.Preload("Orders.Items").Find(&users)

// Joins: single query with a JOIN - for belongs-to / has-one only
db.Joins("Company").Where("users.active = ?", true).Find(&users)
```
`Preload` is the GORM default for collections, so it avoids cartesian explosion. Use `Joins` for to-one relations you also filter on.

## Projection
```go
type UserSummary struct {
    ID   uint
    Name string
}

var out []UserSummary
db.Model(&User{}).Select("id", "name").Where("active = ?", true).Find(&out)
// or: .Scan(&out) for arbitrary joined columns
```
GORM also selects only the fields of the destination struct when you `Find` into a smaller struct with `Model(&User{})`.

## Existence and counting
```go
var n int64
db.Model(&Order{}).Where("status = ?", "pending").Count(&n)

var id uint
err := db.Model(&User{}).Select("id").Where("email = ?", email).Limit(1).Scan(&id).Error
exists := err == nil && id != 0
```
`First` returns `ErrRecordNotFound` (and logs it) for a miss; `Limit(1).Find` doesn't, which suits existence checks.

## Pagination
```go
db.Where("id > ?", lastID).Order("id").Limit(50).Find(&orders)   // keyset
```
Avoid deep `Offset`. Always pair `Limit` with a stable `Order`.

## Writes
```go
db.CreateInBatches(&orders, 500)                       // chunked multi-row INSERT
db.Model(&Order{}).
    Where("created_at < ?", cutoff).
    Update("status", "expired")                        // one UPDATE, no load
db.Model(&Product{}).Where("id = ?", id).
    UpdateColumn("stock", gorm.Expr("stock - ?", 1))  // DB-side arithmetic
```
- `Updates(struct)` ignores zero-value fields; use a `map[string]any` or `Select(...)` to write zeros.
- `gorm.Config{SkipDefaultTransaction: true}` removes the implicit transaction around single writes when you don't need it.
- `gorm.Config{PrepareStmt: true}` caches prepared statements for hot queries.

## Large result sets
```go
db.Where("status = ?", "paid").FindInBatches(&batch, 1000, func(tx *gorm.DB, n int) error {
    // process batch
    return nil
})
```

## Context and pool
- Always `db.WithContext(ctx)` so request cancellation stops the query.
- Tune the pool on the underlying `*sql.DB`: `sqlDB, _ := db.DB(); sqlDB.SetMaxOpenConns(...); sqlDB.SetMaxIdleConns(...); sqlDB.SetConnMaxLifetime(...)`.

## Inspect
- `db.Debug().Find(&users)` prints SQL for one call; `logger.Default.LogMode(logger.Info)` for all.
- `db.ToSQL(func(tx *gorm.DB) *gorm.DB { return tx.Model(&User{}).Where("id = ?", 1).Find(&[]User{}) })` returns SQL without executing.
- Indexes via struct tags: `gorm:"index"` or composite `gorm:"index:idx_orders_customer_created"` on both fields - but prefer versioned migrations over `AutoMigrate` in production.
