# Contoso Cloud — Deploying a Web App

To deploy a web app to Contoso Cloud App Service:

1. Install the CLI: `npm install -g @contoso/ccloud`.
2. Log in with a personal access token: `ccloud login --token <TOKEN>`.
3. From your project folder, run `ccloud deploy`. The CLI builds a container from
   your `Dockerfile` and pushes it to the selected region.
4. By default the app deploys to **US-East**. Use `--region eu-west` or
   `--region ap-south` to change it.

Deployments are **zero-downtime**: Contoso Cloud starts the new version, waits for
its health check at `/healthz` to pass, then switches traffic over. If the health
check fails within 2 minutes, the deployment is automatically rolled back.

Custom domains can be added with `ccloud domain add example.com`, which also
provisions a free TLS certificate.
