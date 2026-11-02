# Publishing

MinVer sets the package version from git history. The tag prefix is `v`. There is no `<Version>` in the project file.

| Git tag on the commit you build | Package version |
| --- | --- |
| `v1.0.0` | `1.0.0` |
| `v1.0.0-preview.1` | `1.0.0-preview.1` |
| none yet (this repo today) | `1.0.0-alpha.0.{height}` |
| commits after `v1.0.0` | `1.0.1-alpha.0.{height}` |

`height` is the number of commits since the latest version tag, or since the root commit when no version tag exists. `MinVerMinimumMajorMinor` is `1.0`, so those untagged builds stay on the 1.0 line instead of `0.0.0-alpha.0`. After `v1.0.0`, MinVer increments the patch for the next untagged commit.

A tag must be SemVer with a patch number: `v1.0.0`, not `v1.0`. The prefix is case-insensitive (`v` or `V`).

This task does not push a package and does not create a tag. Pushing the tag is the release.

## Local pack

From a full clone (tags and history):

```bash
dotnet pack AmiIptvParser/AmiIptvParser.csproj -c Release -o artifacts
```

A shallow clone that does not have tags produces `1.0.0-alpha.0.{height}`. CI checks out with `fetch-depth: 0` so MinVer can see tags.

## Release

1. Update [CHANGELOG.md](../CHANGELOG.md) so the section matches the version you are tagging.
2. Tag the commit to release:

   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

   A prerelease is the same with `v1.0.0-preview.1`. nuget.org lists that as a prerelease; `dotnet add package` skips it unless the consumer opts in.
3. Workflow [`.github/workflows/ci.yml`](../.github/workflows/ci.yml) restores, builds, tests, and packs. On a `v*` tag it checks that `nupkgs/AmiIptvParser.{version}.nupkg` exists, then pushes the package and the symbol package.

Do not push the tag until the trusted-publishing policy and the `NUGET_USER` variable below exist. The publish job fails closed when they are missing, and nothing is uploaded.

Deprecating `0.0.1` and `0.0.2` on nuget.org is a separate manual step (package page → Manage package → Deprecation). The workflow does not do it.

## Trusted publishing (one-time)

The workflow does not use a long-lived NuGet API key. GitHub Actions requests an OIDC token (`id-token: write`). [`NuGet/login`](https://github.com/nuget/login) exchanges it for an API key that lasts about an hour and is used once.

On nuget.org, signed in as the owner of package `AmiIptvParser`:

1. Open your account menu and choose **Trusted Publishing**.
2. Add a policy. The fields are case-insensitive:
   - Repository owner: `amian84`
   - Repository: `AmiIptvParser`
   - Workflow file: `ci.yml` (the file name only, not `.github/workflows/ci.yml`)
   - Environment: leave empty. The workflow does not set a GitHub Actions environment. If you add one later, put the same name here and on the publish job.
3. Choose yourself as the policy owner (the user who owns `AmiIptvParser`). A policy applies to packages owned by that owner.

On GitHub, in `amian84/AmiIptvParser`:

1. Settings → Secrets and variables → Actions → **Variables** (not Secrets).
2. Add `NUGET_USER`. The value is the nuget.org **profile name**, not the email address and not an API key.

No other secret is required. The publish job exits with an error if `NUGET_USER` is empty, before it calls nuget.org.

Confirm the package id `AmiIptvParser` is still owned by that account (`0.0.1` and `0.0.2` were published there). Trusted publishing cannot create the package under a different owner.

Official reference: [Trusted Publishing on nuget.org](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing).
