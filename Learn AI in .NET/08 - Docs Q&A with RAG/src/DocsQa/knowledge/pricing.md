# Contoso Cloud — Plans, Pricing, and Limits

Contoso Cloud offers three plans:

- **Free** — 1 app, 512 MB RAM, sleeps after 30 minutes of inactivity. No custom
  domains. Good for experiments.
- **Pro** — $20 per app per month, 2 GB RAM, always on, unlimited custom domains,
  and a 99.9% uptime SLA.
- **Scale** — $80 per app per month, 8 GB RAM, autoscaling up to 10 instances,
  and a 99.95% uptime SLA.

Managed Postgres is billed separately, starting at $15/month for the Starter
database (1 GB storage) up to $200/month for the Performance database (100 GB).

Rate limits: the API accepts up to **600 requests per minute** per access token.
Requests over the limit receive an HTTP 429 response. Backups are retained for
7 days on all paid database plans and are not available on the Free plan.
