# How to use - Temporal Tables / Auditing Setup

**What it is.** Adds change history to your tables - database-native history (SQL Server temporal tables, MariaDB system versioning, PostgreSQL via the `temporal_tables` extension or a portable trigger-based history table) or an application audit log through ORM hooks (EF Core interceptors, Prisma client extensions, TypeORM subscribers, SQLAlchemy session events, django-simple-history, Hibernate Envers, GORM hooks). It detects your database and ORM first and picks the right approach.

**When to reach for it.** You need record history, point-in-time ("as of") queries, or a who-changed-what audit trail.

**How to use it.** Point it at your project (path or GitHub URL). Example prompts:
- "Add temporal-table history to my Order entity with EF Core."
- "Track who changed what across my Django models."
- "Point-in-time query for this record as of last week."
- "Set up row history on Postgres without extensions."
- "Add Hibernate Envers auditing with the current user."
- "Audit trail for my Prisma models."

**What you get.** The chosen approach and why, setup code (migration SQL, ORM config, hook code), as-of and full-history queries for your main entity, and a retention/PII plan.

**Get the best out of it.** Tell it your database. Say whether you need the actor ("who") - that adds an audit log or actor column. Mention whether writes also happen via raw SQL or bulk updates - that pushes toward native history. Plan retention for history growth.

**Won't do.** It won't decide your PII policy - be explicit about which field values may be stored in history/audit rows.
