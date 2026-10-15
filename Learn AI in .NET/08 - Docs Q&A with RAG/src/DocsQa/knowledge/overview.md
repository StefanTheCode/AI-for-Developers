# Contoso Cloud — Overview

Contoso Cloud is a developer platform for hosting web applications and managed
databases. It is organized into three regions: **US-East**, **EU-West**, and
**AP-South**. Each region is independent, so an outage in one region does not
affect the others.

The platform has two core products:

- **App Service** — runs containerized web apps and APIs. It supports Linux
  containers and scales horizontally.
- **Managed Postgres** — a fully managed PostgreSQL database with automated
  daily backups retained for 7 days.

Every account gets access to the Contoso Cloud CLI (`ccloud`) and a web
dashboard. Authentication uses personal access tokens created from the dashboard.
