# Release signing (Azure Trusted Signing / Artifact Signing)

**Status: deferred.** Releases ship **unsigned** by default. Signing turns on automatically when Azure secrets/vars are configured; until then CI skips sign steps and still publishes Setup.exe + portable zip.

MosaicShell can sign `MosaicShell-Setup-*.exe` plus staged `MosaicShell.Host.exe` and `Mosaicist.exe` via **Azure Trusted Signing** (also branded **Artifact Signing**). Version tags stay **date-build** (`yyyy.M.d-bN`), not semver.

CI: [`.github/workflows/release.yml`](../workflows/release.yml). Local packaging: [`packaging/README.md`](../../packaging/README.md).

There is no free Authenticode option that SmartScreen trusts. Enable this when downloads and publisher trust matter enough to justify Azure’s monthly Artifact Signing fee (~Basic tier).

When you are ready, finish **sections A–C** below. Incomplete config = unsigned release (not a failed job).

---

## A. Azure: Trusted Signing account and certificate

1. Open [Azure Portal](https://portal.azure.com) with a subscription you control.
2. Create an **Artifact Signing** / **Trusted Signing** account in a supported region (examples: East US, West US 2, North Europe). Note the **region**; the signing **endpoint** must match that region.
3. Start an **Identity validation** for your publisher name (individual or organization). Wait until status is **Completed**. You cannot create a usable public certificate profile before this finishes (often days).
4. Create a **Certificate profile** (Public Trust / Code Signing) under that account.
5. Write down:
   - Signing account **name** (resource name, not your Microsoft login)
   - Certificate profile **name**
   - Regional **endpoint** URI from the table below

| Region | Endpoint |
|--------|----------|
| East US | `https://eus.codesigning.azure.net/` |
| West US 2 | `https://wus2.codesigning.azure.net/` |
| West US 3 | `https://wus3.codesigning.azure.net/` |
| West Central US | `https://wcus.codesigning.azure.net/` |
| North Europe | `https://neu.codesigning.azure.net/` |
| West Europe | `https://weu.codesigning.azure.net/` |

A wrong endpoint usually shows up as **403 Forbidden** during sign.

Pricing: Microsoft bills for the account/profile and signing operations. Check current Artifact Signing pricing before enabling on every `main` push.

Official overview: [Azure Artifact Signing](https://learn.microsoft.com/azure/trusted-signing/).

---

## B. Entra app registration (OIDC for GitHub Actions)

Prefer federated credentials (no client secret to rotate).

1. **Microsoft Entra ID** → **App registrations** → **New registration** (e.g. `MosaicShell-GitHub-Signing`).
2. Note **Application (client) ID** and **Directory (tenant) ID**.
3. **Certificates & secrets** → **Federated credentials** → **Add credential**:
   - Scenario: GitHub Actions deploying Azure resources
   - Organization: `uairhahs`
   - Repository: `MosaicShell`
   - Entity type: **Branch**
   - Branch: `main`
4. On the Trusted Signing / Artifact Signing account (or the certificate profile), assign the app the **Trusted Signing Certificate Profile Signer** / **Artifact Signing Certificate Profile Signer** role.
5. Note your Azure **Subscription ID** (Subscriptions blade).

Microsoft guide: [Trusted Signing + GitHub Actions](https://github.com/Azure/trusted-signing-action) (repo may redirect to `artifact-signing-action`).

---

## C. GitHub secrets and variables

Repo: `uairhahs/MosaicShell` → **Settings** → **Secrets and variables** → **Actions**.

### Secrets

| Name | Value |
|------|--------|
| `AZURE_CLIENT_ID` | App registration client ID |
| `AZURE_TENANT_ID` | Directory (tenant) ID |
| `AZURE_SUBSCRIPTION_ID` | Subscription ID |

### Variables (non-secret names/URIs)

| Name | Value |
|------|--------|
| `TRUSTED_SIGNING_ACCOUNT` | Signing account name |
| `TRUSTED_SIGNING_CERT_PROFILE` | Certificate profile name |
| `TRUSTED_SIGNING_ENDPOINT` | Regional endpoint, e.g. `https://eus.codesigning.azure.net/` |

After A–C, run **Actions** → **Release** → **Run workflow**, or push to `main`.

---

## D. What CI does

Order in `release.yml`:

1. `dotnet test`
2. Publish stage (`build-setup.ps1 -SkipInno`)
3. Detect Azure + Trusted Signing config (if incomplete: skip sign steps, continue unsigned)
4. When enabled: `azure/login` (OIDC)
5. When enabled: sign `packaging/stage/Host/MosaicShell.Host.exe` and `packaging/stage/Mosaicist/Mosaicist.exe`
6. Compile Inno (`build-setup.ps1 -SkipPublish`)
7. When enabled: sign `packaging/output/MosaicShell-Setup-{version}.exe` and `signtool verify`
8. Portable zip + `gh release create` (Setup + alias + zip)

Timestamp server used by the action: `http://timestamp.acs.microsoft.com`.

---

## E. Verify locally (optional)

After downloading a release asset:

```powershell
signtool verify /pa /v .\MosaicShell-Setup-*.exe
```

`signtool` comes from the Windows SDK. You do not need to sign on your laptop for v1; CI owns signing so keys never sit on a developer machine.

---

## Expectations

- First public downloads can still hit SmartScreen until publisher reputation builds. Signing is required but not an instant “Unknown publisher” fix for a brand-new identity.
- Identity validation can take days; start section A early.
- Do not commit PFX files or client secrets. OIDC federated credentials only for this workflow.

---

## Checklist

- [ ] Artifact Signing / Trusted Signing account created
- [ ] Identity validation **Completed**
- [ ] Certificate profile created
- [ ] Endpoint matches account region
- [ ] Entra app + federated credential for `uairhahs/MosaicShell` @ `main`
- [ ] App has Certificate Profile Signer role
- [ ] GitHub secrets: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`
- [ ] GitHub variables: `TRUSTED_SIGNING_ACCOUNT`, `TRUSTED_SIGNING_CERT_PROFILE`, `TRUSTED_SIGNING_ENDPOINT`
- [ ] Release workflow run succeeds; `signtool verify` step green
